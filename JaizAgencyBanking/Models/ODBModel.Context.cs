﻿

//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace JaizAgencyBanking.Models
{

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;


public partial class JaizOpenDigitalBankingEntities : DbContext
{
    public JaizOpenDigitalBankingEntities()
        : base("name=JaizOpenDigitalBankingEntities")
    {

    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        throw new UnintentionalCodeFirstException();
    }


    public DbSet<Bill> Bills { get; set; }

    public DbSet<Bills2> Bills2 { get; set; }

    public DbSet<Item> Items { get; set; }

    public DbSet<log> logs { get; set; }

    public DbSet<otprequest> otprequests { get; set; }

    public DbSet<QuickTellerCategories2> QuickTellerCategories2 { get; set; }

    public DbSet<TBFIList> TBFILists { get; set; }

    public DbSet<TransType> TransTypes { get; set; }

    public DbSet<user> users { get; set; }

    public DbSet<vend> vends { get; set; }

    public DbSet<vendingcat> vendingcats { get; set; }

    public DbSet<QuickTellerCategory> QuickTellerCategories { get; set; }

    public DbSet<CategoryBill> CategoryBills { get; set; }

    public DbSet<Items2> Items2 { get; set; }

}

}

