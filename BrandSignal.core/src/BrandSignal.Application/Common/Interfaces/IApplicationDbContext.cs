using BrandSignal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrandSignal.Application.Common.Interaces;

public interface IApplicationDbContext
{
    // DbSet is like a live Excel data sheet of your Campaigns table.
    // The application can read, update, or add rows to this sheet.
    DbSet<Campaign> Campaigns {get;}

    // This promises that the database will have a method to save our changes 
    // in the background without freezing up the user interface.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}