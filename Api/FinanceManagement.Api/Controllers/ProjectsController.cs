using FinanceManagement.Api.Data;
using FinanceManagement.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceManagement.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Projects
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            return await _context.Projects
                .Include(p => p.Manager)
                .ToListAsync();
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return project;
        }

        // GET: api/Projects/5/Transactions
        [HttpGet("{id}/transactions")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetProjectTransactions(int id)
        {
            if (!await _context.Projects.AnyAsync(p => p.Id == id))
            {
                return NotFound();
            }

            return await _context.Transactions
                .Where(t => t.ProjectId == id)
                .Include(t => t.Account)
                .Include(t => t.IncomeCategory)
                .Include(t => t.ExpenseCategory)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        // GET: api/Projects/5/statistics
        [HttpGet("{id}/statistics")]
        public async Task<ActionResult<object>> GetProjectStatistics(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            var transactions = await _context.Transactions
                .Where(t => t.ProjectId == id)
                .ToListAsync();

            var totalIncome = transactions
                .Where(t => t.TransactionType == "Income")
                .Sum(t => t.Amount);

            var totalExpense = transactions
                .Where(t => t.TransactionType == "Expense")
                .Sum(t => t.Amount);

            var profit = totalIncome - totalExpense;
            var budgetUsage = project.Budget > 0 ? (totalExpense / project.Budget) * 100 : 0;

            return new
            {
                totalIncome,
                totalExpense,
                profit,
                budgetUsage,
                transactionCount = transactions.Count,
                projectDetails = new
                {
                    project.Id,
                    project.Name,
                    project.Code,
                    project.Budget,
                    project.Status,
                    project.StartDate,
                    project.EndDate
                }
            };
        }

        // POST: api/Projects
        [HttpPost]
        public async Task<ActionResult<Project>> CreateProject(Project project)
        {
            // 确保管理员存在
            if (!await _context.Users.AnyAsync(u => u.Id == project.ManagerId))
            {
                return BadRequest("指定的项目经理不存在");
            }

            project.CreatedAt = DateTime.Now;
            
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
        }

        // PUT: api/Projects/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, Project project)
        {
            if (id != project.Id)
            {
                return BadRequest();
            }

            // 确保管理员存在
            if (!await _context.Users.AnyAsync(u => u.Id == project.ManagerId))
            {
                return BadRequest("指定的项目经理不存在");
            }

            _context.Entry(project).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(project).Property(p => p.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProjectExists(id))
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

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // 检查项目是否有关联的交易记录
            if (await _context.Transactions.AnyAsync(t => t.ProjectId == id))
            {
                return BadRequest("无法删除已有交易记录的项目");
            }

            // 检查项目是否有关联的合同
            if (await _context.Contracts.AnyAsync(c => c.ProjectId == id))
            {
                return BadRequest("无法删除已有合同的项目");
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> ProjectExists(int id)
        {
            return await _context.Projects.AnyAsync(e => e.Id == id);
        }
    }
} 