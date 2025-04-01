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
    public class ContractsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContractsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Contracts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contract>>> GetContracts()
        {
            return await _context.Contracts
                .Include(c => c.Project)
                .Include(c => c.TaxRate)
                .ToListAsync();
        }

        // GET: api/Contracts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Contract>> GetContract(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Project)
                .Include(c => c.TaxRate)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return NotFound();
            }

            return contract;
        }

        // GET: api/Contracts/Sales
        [HttpGet("Sales")]
        public async Task<ActionResult<IEnumerable<Contract>>> GetSalesContracts()
        {
            return await _context.Contracts
                .Include(c => c.Project)
                .Include(c => c.TaxRate)
                .Where(c => c.ContractType == "Sales")
                .ToListAsync();
        }

        // GET: api/Contracts/Purchase
        [HttpGet("Purchase")]
        public async Task<ActionResult<IEnumerable<Contract>>> GetPurchaseContracts()
        {
            return await _context.Contracts
                .Include(c => c.Project)
                .Include(c => c.TaxRate)
                .Where(c => c.ContractType == "Purchase")
                .ToListAsync();
        }

        // POST: api/Contracts
        [HttpPost]
        public async Task<ActionResult<Contract>> CreateContract(Contract contract)
        {
            // 验证项目是否存在
            if (!await _context.Projects.AnyAsync(p => p.Id == contract.ProjectId))
            {
                return BadRequest("指定的项目不存在");
            }

            // 验证税率是否存在（如果提供了税率ID）
            if (contract.TaxRateId.HasValue && !await _context.TaxRates.AnyAsync(t => t.Id == contract.TaxRateId))
            {
                return BadRequest("指定的税率不存在");
            }

            // 验证客户或供应商是否存在
            if (contract.ContractType == "Sales")
            {
                if (!await _context.Customers.AnyAsync(c => c.Id == contract.CustomerIdOrSupplierId))
                {
                    return BadRequest("指定的客户不存在");
                }
            }
            else if (contract.ContractType == "Purchase")
            {
                if (!await _context.Suppliers.AnyAsync(s => s.Id == contract.CustomerIdOrSupplierId))
                {
                    return BadRequest("指定的供应商不存在");
                }
            }
            else
            {
                return BadRequest("合同类型必须是'Sales'或'Purchase'");
            }

            contract.CreatedAt = DateTime.Now;
            
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContract), new { id = contract.Id }, contract);
        }

        // PUT: api/Contracts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContract(int id, Contract contract)
        {
            if (id != contract.Id)
            {
                return BadRequest();
            }

            // 验证项目是否存在
            if (!await _context.Projects.AnyAsync(p => p.Id == contract.ProjectId))
            {
                return BadRequest("指定的项目不存在");
            }

            // 验证税率是否存在（如果提供了税率ID）
            if (contract.TaxRateId.HasValue && !await _context.TaxRates.AnyAsync(t => t.Id == contract.TaxRateId))
            {
                return BadRequest("指定的税率不存在");
            }

            // 验证客户或供应商是否存在
            if (contract.ContractType == "Sales")
            {
                if (!await _context.Customers.AnyAsync(c => c.Id == contract.CustomerIdOrSupplierId))
                {
                    return BadRequest("指定的客户不存在");
                }
            }
            else if (contract.ContractType == "Purchase")
            {
                if (!await _context.Suppliers.AnyAsync(s => s.Id == contract.CustomerIdOrSupplierId))
                {
                    return BadRequest("指定的供应商不存在");
                }
            }
            else
            {
                return BadRequest("合同类型必须是'Sales'或'Purchase'");
            }

            _context.Entry(contract).State = EntityState.Modified;

            // 保持创建时间不变
            _context.Entry(contract).Property(c => c.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ContractExists(id))
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

        // DELETE: api/Contracts/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteContract(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null)
            {
                return NotFound();
            }

            // 检查合同是否有关联的发票
            if (await _context.Invoices.AnyAsync(i => i.ContractId == id))
            {
                return BadRequest("无法删除已有发票的合同");
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> ContractExists(int id)
        {
            return await _context.Contracts.AnyAsync(e => e.Id == id);
        }
    }
} 