using Bogus;
using Microsoft.EntityFrameworkCore;
using WebSimba.Data;
using WebSimba.Data.Entities;
using WebSimba.Mapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(typeof(AppMapperProfile));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

//Dependecy Injection
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate(); //Запусти міграції на БД, якщо їх там немає

    if(!dbContext.Categories.Any())
    {
        const int number = 10;
        var categories = new Faker("uk").Commerce
            .Categories(number);
        foreach (var name in categories)
        {
            var entity = dbContext.Categories.SingleOrDefault(c => c.Name == name);
            if(entity != null)
                continue;

            entity = new CategoryEntity
            {
                Name = name
            };
            dbContext.Categories.Add(entity);
            dbContext.SaveChanges();
        }
    }
}

app.Run();
