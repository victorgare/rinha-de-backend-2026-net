var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RinhaNet_Api>("rinhanet-api");

builder.Build().Run();
