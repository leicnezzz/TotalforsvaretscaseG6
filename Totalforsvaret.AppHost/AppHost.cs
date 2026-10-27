var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Totalforsvaret>("totalforsvaret");

builder.Build().Run();