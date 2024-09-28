using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebSimba.Data;
using WebSimba.Data.Entities;
using WebSimba.Models.Category;

namespace WebSimba.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(ApplicationDbContext _context,
        IMapper mapper) : ControllerBase
    {
        //private readonly ApplicationDbContext _context;

        //public CategoryController(ApplicationDbContext context)
        //{
        //    _context = context;
        //}

        // GET: api/Category
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var model = await _context.Categories
                .ProjectTo<CategoryItemModel>(mapper.ConfigurationProvider)
                .ToListAsync();
            return Ok(model);
        }

        // GET: api/Category/2
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryEntity>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return category;
        }

        // POST: api/Category
        [HttpPost]
        public async Task<ActionResult<CategoryEntity>> PostCategory(CategoryEntity category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        // PUT: api/Category/2
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, CategoryEntity category)
        {
            if (id != category.Id)
            {
                return BadRequest();
            }

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        // DELETE: api/Category/2
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}
