
using LabDash.Areas.Identity.Data;
using LabDash.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<LabDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


builder.Services.AddIdentity<LabUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<LabDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
name: "default",
pattern: "{controller=Home}/{action=Index}/{id?}");


using (var scope = app.Services.CreateScope())
{


    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LabDbContext>();

    // Initialize RoleManager
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();// look in to

    // Check if "Admin" role exists, if not, create it
    if (!roleManager.RoleExistsAsync("Admin").Result)
    {
        var role = new IdentityRole("Admin");
        roleManager.CreateAsync(role).Wait();
    }

    // Check if "Nurse" role exists, if not, create it
    if (!roleManager.RoleExistsAsync("Patient").Result)
    {
        var role = new IdentityRole("Patient");
        roleManager.CreateAsync(role).Wait();
    }

    // Check if "Surgeon" role exists, if not, create it
    if (!roleManager.RoleExistsAsync("Lab_Manager").Result)
    {
        var role = new IdentityRole("Lab_Manager");
        roleManager.CreateAsync(role).Wait();
    }

    // Check if "Pharmacist" role exists, if not, create it
    if (!roleManager.RoleExistsAsync("Doctor").Result)
    {
        var role = new IdentityRole("Doctor");
        roleManager.CreateAsync(role).Wait();
    }

    // Check if "Anaesthesiologist" role exists, if not, create it
    if (!roleManager.RoleExistsAsync("Lab_Technician").Result)
    {
        var role = new IdentityRole("Lab_Technician");
        roleManager.CreateAsync(role).Wait();
    }
}
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<LabUser>>();

    string email = "admin@gmail.com";
    string password = "Password!123";

    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new LabUser();
        user.Email = email;
        user.UserName = email;
        user.EmailConfirmed = true;
        user.FirstName = "Phindulo";
        user.LastName = "Rasimphi";
        //user.HCRN = 0;
        user.PhoneNumb = "0741234567";
        user.Gender = "Male";

        user.Timestamp_AccountCreated = DateTime.Now;
        await userManager.CreateAsync(user, password);

        await userManager.AddToRoleAsync(user, "Admin");
    }
}
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<LabUser>>();

    string email = "patient@gmail.com";
    string password = "Password!123";

    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new LabUser();
        user.Email = email;
        user.UserName = email;
        user.EmailConfirmed = true;
        user.FirstName = "Thabo";
        user.LastName = "Mokoena";
        //user.HCRN = 0;
        user.PhoneNumb = "0732451097";
        user.Gender = "Male";

        user.Timestamp_AccountCreated = DateTime.Now;
        await userManager.CreateAsync(user, password);

        await userManager.AddToRoleAsync(user, "Patient");
    }
}
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<LabUser>>();

    string email = "doctor@gmail.com";
    string password = "Password!123";

    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new LabUser();
        user.Email = email;
        user.UserName = email;
        user.EmailConfirmed = true;
        user.FirstName = "Kamogelo";
        user.LastName = "Makuwa";
        //user.HCRN = 0;
        user.PhoneNumb = "0867501735";
        user.Gender = "Female";

        user.Timestamp_AccountCreated = DateTime.Now;
        await userManager.CreateAsync(user, password);

        await userManager.AddToRoleAsync(user, "Doctor");
    }
}
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<LabUser>>();

    string email = "labtech@gmail.com";
    string password = "Password!123";

    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new LabUser();
        user.Email = email;
        user.UserName = email;
        user.EmailConfirmed = true;
        user.FirstName = "Siyolise";
        user.LastName = "Sipika";
        //user.HCRN = 0;
        user.PhoneNumb = "0819859207";
        user.Gender = "Female";

        user.Timestamp_AccountCreated = DateTime.Now;
        await userManager.CreateAsync(user, password);

        await userManager.AddToRoleAsync(user, "Lab_Technician");
    }
}
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<LabUser>>();

    string email = "labmanager@gmail.com";
    string password = "Password!123";

    if (await userManager.FindByEmailAsync(email) == null)
    {
        var user = new LabUser();
        user.Email = email;
        user.UserName = email;
        user.EmailConfirmed = true;
        user.FirstName = "Lusanda";
        user.LastName = "Mkhize";
        //user.HCRN = 0;
        user.PhoneNumb = "0639278012";
        user.Gender = "Female";

        user.Timestamp_AccountCreated = DateTime.Now;
        await userManager.CreateAsync(user, password);

        await userManager.AddToRoleAsync(user, "Lab_Manager");
    }
}
app.Run();

