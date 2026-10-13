// PROGRAM.CS: the starting point. Everything the app needs is wired up here.
//
// Interview line: "Program.cs builds the app (services + configuration), then defines
// the request pipeline (what happens to each HTTP request)."

using Azure.Identity;

// Step 1: create the builder. Think of it as a "set up the restaurant" object.
// It loads configuration (appsettings.json, environment variables) automatically.
var builder = WebApplication.CreateBuilder(args);

// ---- STAGE 4 (Key Vault) ----
// WHY: secrets (passwords, keys, messages we want protected) must never live in code or
// in files committed to GitHub. Key Vault is the safe; this code lets the app read from it.
// It only switches on when KeyVault:Uri has a value, so Stages 1-3 work without Azure.
// DefaultAzureCredential tries several ways to log in, in order. On your laptop it uses your
// "az login" identity; inside AKS it uses the workload identity. SAME CODE, NO PASSWORD.
var vaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(vaultUri))
{
    // Secrets from the vault are added to configuration, so _config["HelloMessage"]
    // in the controller reads the vault secret named "HelloMessage".
    builder.Configuration.AddAzureKeyVault(new Uri(vaultUri), new DefaultAzureCredential());
}

// Step 2: register services (the app's toolbox).
builder.Services.AddControllers();          // lets us use controller classes like HelloController
builder.Services.AddEndpointsApiExplorer(); // discovers our endpoints so Swagger can list them
builder.Services.AddSwaggerGen();           // generates the OpenAPI description (also used by APIM)

// Step 3: build the app. After this line we can no longer add services.
var app = builder.Build();

// Step 4: define the pipeline (what happens to every request, in order).
// Swagger is on in all environments so you can also test it inside AKS.
// In a real production API you would normally restrict this.
app.UseSwagger();    // serves the OpenAPI JSON at /swagger/v1/swagger.json
app.UseSwaggerUI();  // serves the test page at /swagger

app.MapControllers(); // connect URLs to the controller methods

// ---- Minimal API style ----
// The same idea as a controller, written in one line. Compare with HelloController.cs.
// WHY /health: Kubernetes calls it to ask "are you alive and ready?" (see k8s/deployment.yaml).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Step 5: start listening for requests.
app.Run();
