using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Diagnostics;
using WebSimba.Constants;
using WebSimba.Data;
using WebSimba.Data.Entities;
using WebSimba.Data.Entities.Identity;
using WebSimba.Interfaces;
using WebSimba.Mapper;
using WebSimba.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<UserEntity, RoleEntity>(options =>
{
    options.Stores.MaxLengthForKeys = 128;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 5;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAutoMapper(typeof(AppMapperProfile));

builder.Services.AddScoped<IImageWorker, ImageWorker>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(opt =>
    opt.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseAuthorization();

app.MapControllers();

var dir = builder.Configuration["ImageDir"];
Console.WriteLine("-------Image dir {0}-------", dir);
var dirPath = Path.Combine(Directory.GetCurrentDirectory(), dir);
if(!Directory.Exists(dirPath))
    Directory.CreateDirectory(dirPath);

//app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(dirPath),
    RequestPath = "/images"
});

var imageNo = Path.Combine(dirPath, "noimage.jpg");
if(!File.Exists(imageNo))
{
    string url = "https://m.media-amazon.com/images/I/71QaVHD-ZDL.jpg";
    try
    {
        using (HttpClient client = new HttpClient())
        {
            // Send a GET request to the image URL
            HttpResponseMessage response = client.GetAsync(url).Result;

            // Check if the response status code indicates success (e.g., 200 OK)
            if (response.IsSuccessStatusCode)
            {
                // Read the image bytes from the response content
                byte[] imageBytes = response.Content.ReadAsByteArrayAsync().Result;
                File.WriteAllBytes(imageNo, imageBytes);
            }
            else
            {
                Console.WriteLine($"------Failed to retrieve image. Status code: {response.StatusCode}---------");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"-----An error occurred: {ex.Message}------");
    }
}

//Dependecy Injection
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var imageWorker = scope.ServiceProvider.GetRequiredService<IImageWorker>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<RoleEntity>>();


    dbContext.Database.Migrate(); //Запусти міграції на БД, якщо їх там немає
    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();

    if (!dbContext.Categories.Any())
    {
        const int number = 10;
        var categories = new Faker("uk").Commerce
            .Categories(number);
        foreach (var name in categories)
        {
            var entity = dbContext.Categories.SingleOrDefault(c => c.Name == name);
            if (entity != null)
                continue;

            string image = imageWorker.Save("https://picsum.photos/1200/800?category").Result;
            entity = new CategoryEntity
            {
                Name = name,
                Image = image
            };
            dbContext.Categories.Add(entity);
            dbContext.SaveChanges();
        }
    }

    if(!dbContext.Products.Any())
    {
        var catIds = dbContext.Categories.Select(x => x.Id).ToArray();
        const int productNumbers = 30;
        var faker = new Faker();
        for (int i = 0; i < productNumbers; i++) {
            
            var product = new ProductEntity
            {
                Name = faker.Commerce.ProductName(),
                CategoryId = faker.PickRandom(catIds),
                Price = Decimal.Parse(faker.Commerce.Price(100, 3000))
            };
            int imageCount = faker.Random.Number(3, 6);
            for (int j = 0; j < imageCount; j++)
            {
                string image = imageWorker.Save("https://picsum.photos/1200/800?product").Result;
                ProductImageEntity pi = new ProductImageEntity
                {
                    Priority = j, 
                    Image = image,
                    Product = product
                };
                dbContext.ProductImages.Add(pi);
            }
            dbContext.Products.Add(product);
            dbContext.SaveChanges();
        }
    }

    if (!dbContext.Roles.Any())
    {
        foreach (var role in Roles.GetAll)
        {
            var result = roleManager.CreateAsync(new RoleEntity { Name = role }).Result;
            if(!result.Succeeded)
            {
                Console.WriteLine($"--Error create Role {role}--");
            }
        }
    }

    if (!dbContext.Users.Any())
    {
        string image = imageWorker.Save("https://picsum.photos/1200/800?person").Result;
        var user = new UserEntity
        {
            Email="admin@gmail.com",
            UserName="admin@gmail.com",
            LastName="Підкаблучник",
            FirstName="Іван",
            Image= image
        };
        var result = userManager.CreateAsync(user,"123456").Result;
        if(!result.Succeeded)
        {
            Console.WriteLine($"--Problem create user--{user.Email}");
        }
        else
        {
            result = userManager.AddToRoleAsync(user, Roles.Admin).Result;
        }
    }

    stopWatch.Stop();
    // Get the elapsed time as a TimeSpan value.
    TimeSpan ts = stopWatch.Elapsed;

    // Format and display the TimeSpan value.
    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
        ts.Hours, ts.Minutes, ts.Seconds,
        ts.Milliseconds / 10);
    Console.WriteLine("-----------------Seed Conpleted------------- " + elapsedTime);
}

app.Run();
