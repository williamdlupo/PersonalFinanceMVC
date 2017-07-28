namespace PersonalFinance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TableIDUpdate : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.AspNetUsers", "TableID", c => c.Int(nullable: false, identity: true));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.AspNetUsers", "TableID", c => c.Int(nullable: false));
        }
    }
}
