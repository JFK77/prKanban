# Script to create an admin user in the prKanban database
# Usage: Run this script and enter the password when prompted.

$baseUrl = "http://localhost:5000"
$adminEmail = "admin@prkanban.local"
$securePassword = Read-Host "Admin password" -AsSecureString
$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $adminPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
}
if ([string]::IsNullOrWhiteSpace($adminPassword)) {
    throw "An admin password is required."
}
$adminDisplayName = "Admin User"

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              Creating ADMIN User in prKanban                  ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

# Step 1: Check if backend is running
Write-Host "🔍 Checking backend connectivity..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/v1/health" -Method GET -ErrorAction SilentlyContinue -TimeoutSec 5
    Write-Host "✅ Backend is running and responsive!" -ForegroundColor Green
} catch {
    Write-Host "❌ ERROR: Backend is NOT responding!" -ForegroundColor Red
    Write-Host "   Please ensure backend is running on $baseUrl" -ForegroundColor Yellow
    Write-Host "   Command: cd Backend && dotnet run" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Step 2: Register the admin user
Write-Host "📝 Registering admin user..." -ForegroundColor Cyan
Write-Host "   Email: $adminEmail" -ForegroundColor Gray
Write-Host ""

$registerPayload = @{
    email = $adminEmail
    password = $adminPassword
    confirmPassword = $adminPassword
    displayName = $adminDisplayName
} | ConvertTo-Json

try {
    $registerResponse = Invoke-WebRequest -Uri "$baseUrl/auth/register" `
        -Method POST `
        -ContentType "application/json" `
        -Body $registerPayload `
        -ErrorAction SilentlyContinue

    if ($registerResponse.StatusCode -eq 200 -or $registerResponse.StatusCode -eq 201) {
        Write-Host "✅ Admin user registered successfully!" -ForegroundColor Green
        $registerData = $registerResponse.Content | ConvertFrom-Json
        Write-Host "   Response: $($registerData.message)" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  Unexpected response: $($registerResponse.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    $errorResponse = $_.Exception.Response
    if ($errorResponse.StatusCode -eq 400) {
        Write-Host "⚠️  User might already exist or validation error" -ForegroundColor Yellow
        try {
            $errorContent = $errorResponse.Content.ReadAsStream() | ForEach-Object {
                [System.IO.StreamReader]::new($_).ReadToEnd()
            }
            Write-Host "   Details: $errorContent" -ForegroundColor Gray
        } catch {
            Write-Host "   (Could not read error details)" -ForegroundColor Gray
        }
    } else {
        Write-Host "❌ Registration failed: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# Step 3: Login with the admin user
Write-Host "🔐 Logging in with admin credentials..." -ForegroundColor Cyan

$loginPayload = @{
    email = $adminEmail
    password = $adminPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest -Uri "$baseUrl/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginPayload `
        -ErrorAction SilentlyContinue `
        -SessionVariable "webSession"

    if ($loginResponse.StatusCode -eq 200) {
        Write-Host "✅ Login successful!" -ForegroundColor Green
        $loginData = $loginResponse.Content | ConvertFrom-Json
        Write-Host "   User ID: $($loginData.data.id)" -ForegroundColor Gray
        Write-Host "   Email: $($loginData.data.email)" -ForegroundColor Gray
        Write-Host "   Display Name: $($loginData.data.displayName)" -ForegroundColor Gray
        Write-Host "   Auth Token: Set in httpOnly cookie" -ForegroundColor Gray
    } else {
        Write-Host "❌ Login failed with status $($loginResponse.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Login error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                    ✅ SETUP COMPLETE!                         ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Login Credentials:" -ForegroundColor Cyan
Write-Host "   Email:    $adminEmail" -ForegroundColor Yellow
Write-Host ""
Write-Host "🌐 Access the app at: http://localhost:3000" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 Test Steps:" -ForegroundColor Cyan
Write-Host "   1. Open http://localhost:3000" -ForegroundColor Gray
Write-Host "   2. Click Login" -ForegroundColor Gray
Write-Host "   3. Enter email: $adminEmail" -ForegroundColor Gray
Write-Host "   4. Enter the password you supplied" -ForegroundColor Gray
Write-Host "   5. Click Login button" -ForegroundColor Gray
Write-Host "   6. You should see the Kanban board!" -ForegroundColor Gray
Write-Host ""
