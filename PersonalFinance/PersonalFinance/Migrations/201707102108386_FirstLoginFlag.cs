namespace PersonalFinance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FirstLoginFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "FirstLoginFlag", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "FirstLoginFlag");
        }
    }
}
