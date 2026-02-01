using AbraqAccount.Models;
using Microsoft.EntityFrameworkCore;

namespace AbraqAccount.Data;

public static class DbInitializer
{
    public static async Task Initialize(AppDbContext context)
    {
        // Ensure database is created (handled by migrations usually, but safe to check)
        // context.Database.EnsureCreated();

        // 1. Seed Users
        if (!context.Users.Any())
        {
            context.Users.Add(new User { Username = "admin", Password = "admin" });
        }

        // 2. Seed Master Groups
        if (!context.MasterGroups.Any())
        {
            context.MasterGroups.AddRange(
                new MasterGroup { Name = "ASSETS", Code = "AST", CreatedAt = DateTime.Now },
                new MasterGroup { Name = "LIABILITIES", Code = "LIB", CreatedAt = DateTime.Now },
                new MasterGroup { Name = "INCOME", Code = "INC", CreatedAt = DateTime.Now },
                new MasterGroup { Name = "EXPENSES", Code = "EXP", CreatedAt = DateTime.Now }
            );
            await context.SaveChangesAsync();
        }

        var assetsGroup = await context.MasterGroups.FirstOrDefaultAsync(g => g.Name == "ASSETS");
        var incomeGroup = await context.MasterGroups.FirstOrDefaultAsync(g => g.Name == "INCOME");

        // 3. Seed Master Sub Groups
        if (!context.MasterSubGroups.Any() && assetsGroup != null && incomeGroup != null)
        {
            context.MasterSubGroups.AddRange(
                new MasterSubGroup { Name = "Current Assets", MasterGroupId = assetsGroup.Id, CreatedAt = DateTime.Now },
                new MasterSubGroup { Name = "Direct Income", MasterGroupId = incomeGroup.Id, CreatedAt = DateTime.Now }
            );
            await context.SaveChangesAsync();
        }

        var currentAssets = await context.MasterSubGroups.FirstOrDefaultAsync(s => s.Name == "Current Assets");
        var directIncome = await context.MasterSubGroups.FirstOrDefaultAsync(s => s.Name == "Direct Income");

        // 4. Seed Sub Group Ledgers
        if (!context.SubGroupLedgers.Any() && assetsGroup != null && currentAssets != null && incomeGroup != null && directIncome != null)
        {
            context.SubGroupLedgers.AddRange(
                new SubGroupLedger { Name = "Bank Accounts", MasterGroupId = assetsGroup.Id, MasterSubGroupId = currentAssets.Id, CreatedAt = DateTime.Now },
                new SubGroupLedger { Name = "Cash In Hand", MasterGroupId = assetsGroup.Id, MasterSubGroupId = currentAssets.Id, CreatedAt = DateTime.Now },
                new SubGroupLedger { Name = "Sale of Produce", MasterGroupId = incomeGroup.Id, MasterSubGroupId = directIncome.Id, CreatedAt = DateTime.Now }
            );
            await context.SaveChangesAsync();
        }

        var bankLedger = await context.SubGroupLedgers.FirstOrDefaultAsync(l => l.Name == "Bank Accounts");
        var saleLedger = await context.SubGroupLedgers.FirstOrDefaultAsync(l => l.Name == "Sale of Produce");

        // 5. Seed Bank Masters
        if (!context.BankMasters.Any() && bankLedger != null)
        {
            context.BankMasters.Add(new BankMaster 
            { 
                AccountName = "HDFC Bank", 
                GroupId = bankLedger.Id, 
                AccountNumber = "50100123456789", 
                IfscCode = "HDFC0001234",
                Status = "Active",
                CreatedDate = DateTime.Now
            });
            await context.SaveChangesAsync();
        }

        // 6. Seed Grower Groups
        if (!context.GrowerGroups.Any())
        {
            context.GrowerGroups.Add(new GrowerGroup 
            { 
                GroupCode = "GRP001", 
                GroupName = "Abraq Main Group", 
                GroupType = "FPO", 
                BillingMode = "GROUP",
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }

        var mainGroup = await context.GrowerGroups.FirstOrDefaultAsync(g => g.GroupCode == "GRP001");

        // 7. Seed Farmers
        if (!context.Farmers.Any() && mainGroup != null)
        {
            context.Farmers.Add(new Farmer 
            { 
                FarmerCode = "FRM001", 
                FarmerName = "John Doe", 
                GroupId = mainGroup.Id, 
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }

        var sampleFarmer = await context.Farmers.FirstOrDefaultAsync(f => f.FarmerCode == "FRM001");

        // 8. Seed Vendors
        if (!context.Vendors.Any())
        {
            context.Vendors.Add(new Vendor 
            { 
                VendorCode = "VND001", 
                VendorName = "Agro Seeds Pvt Ltd", 
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }

        var sampleVendor = await context.Vendors.FirstOrDefaultAsync(v => v.VendorCode == "VND001");

        // 9. Seed Sample Transactions to test Unit filtering
        if (!context.GeneralEntries.Any() && bankLedger != null && saleLedger != null)
        {
            context.GeneralEntries.AddRange(
                new GeneralEntry 
                { 
                    VoucherNo = "GE001", 
                    EntryDate = DateTime.Now,
                    DebitAccountId = bankLedger.Id,
                    DebitAccountType = "SubGroupLedger",
                    CreditAccountId = saleLedger.Id,
                    CreditAccountType = "SubGroupLedger",
                    Amount = 1000,
                    Unit = "UNIT-1",
                    Status = "Approved",
                    CreatedAt = DateTime.Now
                },
                new GeneralEntry 
                { 
                    VoucherNo = "GE002", 
                    EntryDate = DateTime.Now,
                    DebitAccountId = bankLedger.Id,
                    DebitAccountType = "SubGroupLedger",
                    CreditAccountId = saleLedger.Id,
                    CreditAccountType = "SubGroupLedger",
                    Amount = 2500,
                    Unit = "UNIT-2",
                    Status = "Approved",
                    CreatedAt = DateTime.Now
                }
            );
        }



        await context.SaveChangesAsync();
    }
}
