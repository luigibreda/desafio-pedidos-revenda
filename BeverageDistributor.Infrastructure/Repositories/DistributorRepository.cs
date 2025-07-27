using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Interfaces;
using BeverageDistributor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeverageDistributor.Infrastructure.Repositories
{
    public class DistributorRepository : IDistributorRepository
    {
        private readonly ApplicationDbContext _context;

        public DistributorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Distributor> AddAsync(Distributor distributor)
        {
            await _context.Distributors.AddAsync(distributor);
            await _context.SaveChangesAsync();
            return distributor;
        }

        public async Task DeleteAsync(Guid id)
        {
            var distributor = await GetByIdAsync(id);
            if (distributor != null)
            {
                _context.Distributors.Remove(distributor);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Distributor>> GetAllAsync()
        {
            return await _context.Distributors
                .Include(d => d.PhoneNumbers)
                .Include(d => d.ContactNames)
                .Include(d => d.Addresses)
                .ToListAsync();
        }

        public async Task<Distributor?> GetByIdAsync(Guid id)
        {
            return await _context.Distributors
                .AsNoTracking()
                .Include(d => d.PhoneNumbers)
                .Include(d => d.ContactNames)
                .Include(d => d.Addresses)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Distributor?> GetByCnpjAsync(string cnpj)
        {
            return await _context.Distributors
                .AsNoTracking()
                .Include(d => d.PhoneNumbers)
                .Include(d => d.ContactNames)
                .Include(d => d.Addresses)
                .FirstOrDefaultAsync(d => d.Cnpj == cnpj);
        }

        public async Task UpdateAsync(Distributor distributor)
        {
            _context.Entry(distributor).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }
    }
}
