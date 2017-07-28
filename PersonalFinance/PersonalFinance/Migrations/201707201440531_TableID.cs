namespace PersonalFinance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TableID : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "TableId", c => c.Int(nullable: false, identity: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "TableId");
        }
    }
}
