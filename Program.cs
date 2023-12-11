var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseAuthorization();
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.Run();
