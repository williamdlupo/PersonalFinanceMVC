namespace PersonalFinance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TableIDUpdate3 : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.AspNetUsers", "TableID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AspNetUsers", "TableID", c => c.Int(nullable: false, identity: true));
        }
    }
}
