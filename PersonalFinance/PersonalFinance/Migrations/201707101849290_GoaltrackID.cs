namespace PersonalFinance.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class GoaltrackID : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "GoaltrackID", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "GoaltrackID");
        }
    }
}
