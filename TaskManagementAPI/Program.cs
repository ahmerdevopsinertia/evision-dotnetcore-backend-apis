using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskManagementAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
{
	loggerConfiguration
		.ReadFrom.Configuration(hostingContext.Configuration)
		.Enrich.FromLogContext()
		.WriteTo.Console();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new() { Title = "Address Management Sample APIs", Version = "v1" });
});

// Add DbContext with PostgreSQL
builder.Services.AddDbContext<BiodataContext>(options =>
		options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔥 Add CORS services
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowFrontend",
		policy =>
		{
			policy.AllowAnyOrigin()
			.AllowAnyMethod().
			AllowAnyHeader();
		});
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskManagementAPI v1"));
}

app.UseHttpsRedirection();

app.UseExceptionHandler(errorApp =>
{
	errorApp.Run(async context =>
	{
		context.Response.StatusCode = 500;
		context.Response.ContentType = "application/json";

		var errorFeature = context.Features.Get<IExceptionHandlerFeature>();
		if (errorFeature != null)
		{
			var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
			logger.LogError(errorFeature.Error, "An unhandled exception has occurred while executing the request");
		}

		await context.Response.WriteAsync("An error occurred while processing your request. Please try again later.");
	});
});

// CRUD opertions for Address
app.MapGet("/address", (BiodataContext db) =>
{
	var result = db.Addresses.Select(a => new AddressDto { Id = a.Id, StreetAddress = a.StreetAddress, City = a.City }).ToListAsync();
	return Results.Ok(new { result });
})
.WithName("getAllAddresses");

app.MapGet("/addressPaginated", async (int page, int pageSize, string? searchTerm, BiodataContext db) =>
{
	if (!string.IsNullOrWhiteSpace(searchTerm))
	{
		searchTerm = searchTerm.ToLower();

		var query = db.Addresses.AsQueryable();

		query = query.Where(a => a.StreetAddress.ToLower().Contains(searchTerm) || a.City.ToLower().Contains(searchTerm));

		var totalCount = await query.CountAsync();

		var items = await query
			 .Select(a => new AddressDto
			 {
				 Id = a.Id,
				 StreetAddress = a.StreetAddress,
				 City = a.City
			 })
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

		return Results.Ok(new
		{
			totalCount,
			items
		});
	}
	else
	{
		var query = db.Addresses.Select(a => new AddressDto { Id = a.Id, StreetAddress = a.StreetAddress, City = a.City });

		var totalCount = await query.CountAsync();

		var items = await query
		.Skip((page - 1) * pageSize)
		.Take(pageSize)
		.ToListAsync();

		return Results.Ok(new
		{
			totalCount,
			items
		});
	}
})
.WithName("getAllAddressesPaginated");

app.MapGet("/address/{id}", async (Guid id, BiodataContext db) =>
{
	var address = await db.Addresses.FindAsync(id);
	return address is not null ? Results.Ok(address) : Results.NotFound();
})
.WithName("getSingleAddress");

app.MapPost("/address", async (Address newAddress, BiodataContext db) =>
{
	Console.WriteLine("New Address: ");
	Console.WriteLine(newAddress);

	db.Addresses.Add(newAddress);
	await db.SaveChangesAsync();
	return Results.Created($"/address/{newAddress.Id}", newAddress);
})
.WithName("addNewAddress");

app.MapPut("/address/{id}", async (Guid id, Address updateAddress, BiodataContext db) =>
{
	var address = await db.Addresses.FindAsync(id);
	if (address is null)
	{
		return Results.NotFound();
	}

	address.StreetAddress = updateAddress.StreetAddress;
	address.City = updateAddress.City;
	address.ZipCode = updateAddress.ZipCode;

	await db.SaveChangesAsync();
	return Results.NoContent();
})
.WithName("updateAddress");

app.MapDelete("/address/{id}", async (Guid id, BiodataContext db) =>
{
	var address = await db.Addresses.FindAsync(id);
	if (address is null)
	{
		return Results.NotFound();
	}

	db.Addresses.Remove(address);
	await db.SaveChangesAsync();
	return Results.NoContent();
})
.WithName("deleteAddress");

app.MapGet("/city", (BiodataContext db) =>
{
	var cities = new List<City> {
		new City { Id = "1", Name = "Dubai" },
		new City { Id = "2", Name = "Abu Dhabi" },
		new City { Id = "3", Name = "Sharjah" },
		new City { Id = "4", Name = "Ajman" },
		new City { Id = "5", Name = "Fujairah" }
	};
	return Results.Ok(cities);
})
.WithName("getAllCities");


app.MapGet("/error", () =>
{
	throw new Exception("Test exception for logging!");
}).WithName("error");

// Execute the application
app.Run();