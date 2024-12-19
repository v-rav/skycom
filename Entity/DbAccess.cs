//=============================================================================
// DbAccess
// EntityFrameworkCoreからDBアクセスするためのクラス
// 
// Copyright (C) SKYCOM Corporation
// EandM
//=============================================================================

using Microsoft.EntityFrameworkCore;

namespace SKYCOM.DLManagement.Entity
{
    public class DbAccess : DbContext
    {
        public virtual DbSet<DLStatusManagements> DLStatusManagements { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<Product> Product { get; set; }

        public DbAccess() { }
        public DbAccess(DbContextOptions<DbAccess> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
}
