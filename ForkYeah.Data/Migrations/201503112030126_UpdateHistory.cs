namespace ForkYeah.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateHistory : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UpdateHistories",
                c => new
                    {
                        StartTime = c.DateTimeOffset(nullable: false, precision: 7),
                        EndTime = c.DateTimeOffset(precision: 7),
                        TotalCount = c.Int(nullable: false),
                        UpdatedCount = c.Int(),
                        Exception = c.String(),
                    })
                .PrimaryKey(t => t.StartTime);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.UpdateHistories");
        }
    }
}
