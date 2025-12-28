# Microsoft Sign-In with MSAL Authentication

This application uses Microsoft Authentication Library (MSAL) for Microsoft Sign-In, supporting both personal Microsoft accounts and work/school accounts.

## ⚠️ CRITICAL: Fixing "client_secret" Error

If you see the error **"The provided request must include a 'client_secret' input parameter"**, your app is configured incorrectly:

### Quick Fix:
1. Go to [Azure Portal](https://portal.azure.com) → **App registrations** → Your app
2. Click **Authentication** in the left menu
3. Under **Platform configurations**:
   - If you see "Web" with your redirect URI, **DELETE IT**
   - Click **Add a platform** → Select **Single-page application**
   - Add redirect URI: `http://localhost:1234/auth`
   - Add another redirect URI: `http://localhost:1234/logout`
   - Click **Configure**
4. Scroll down to **Advanced settings**
5. Under **Allow public client flows**:
   - Set **"Enable the following mobile and desktop flows"** to **Yes**
6. Click **Save**
7. Wait 5 minutes for changes to propagate, then try logging in again

**Why this happens:** Browser apps (SPAs) cannot securely store client secrets. The app must be registered as a "Single-page application", not a "Web" application.

---

## Step-by-Step Setup Guide

### Step 1: Register Your Application in Azure Portal

1. **Navigate to Azure Portal:**
   - Go to [https://portal.azure.com](https://portal.azure.com)
   - Sign in with your Microsoft account

2. **Access App Registrations:**
   - In the left sidebar, click on "Azure Active Directory" (or search for it in the top search bar)
   - In the Azure AD menu, click on "App registrations"
   - Click the "New registration" button at the top

3. **Configure Basic Settings:**
   - **Name**: Enter a name for your application (e.g., "Micro-Frontend Admin Portal")
   - **Supported account types**: Select **"Accounts in any organizational directory and personal Microsoft accounts (e.g., Skype, Xbox)"**
     - This allows both personal Microsoft accounts (@outlook.com, @hotmail.com) and work/school accounts
   - **Redirect URI**:
     - From the dropdown, select **"Single-page application (SPA)"**
     - Enter: `http://localhost:1234/auth`
   - Click **"Register"** button

4. **Copy Your Application (Client) ID:**
   - After registration, you'll be taken to the app's Overview page
   - Copy the **Application (client) ID** (it's a GUID like `12345678-1234-1234-1234-123456789abc`)
   - Keep this handy - you'll need it in Step 2

### Step 2: Configure Your Application

1. **Update the .env file:**
   - Open `packages/shell/.env` in your code editor
   - Replace `YOUR_CLIENT_ID_HERE` with your actual Application (client) ID:
   ```env
   VITE_MSFT_CLIENT_ID=12345678-1234-1234-1234-123456789abc
   ```
   - Save the file

2. **Verify other settings** (these should already be correct):
   ```env
   VITE_MSFT_AUTHORITY=https://login.microsoftonline.com/consumers
   VITE_MSFT_REDIRECT_URI=http://localhost:1234/auth
   VITE_MSFT_POST_LOGOUT_REDIRECT_URI=http://localhost:1234/logout
   ```

### Step 3: Configure API Permissions (Optional but Recommended)

1. **Access API Permissions:**
   - In Azure Portal, go back to your app registration
   - Click on "API permissions" in the left menu

2. **Verify User.Read Permission:**
   - You should see "User.Read" permission already listed under Microsoft Graph
   - If not present, click "Add a permission"
   - Select "Microsoft Graph" > "Delegated permissions"
   - Search for and select "User.Read"
   - Click "Add permissions"

3. **Grant Consent (if required):**
   - If you see a yellow warning icon, you may need admin consent
   - For personal development, User.Read typically doesn't require admin consent
   - Users will consent when they first sign in

### Step 4: Add Additional Redirect URIs for Production (Optional)

1. **Navigate to Authentication:**
   - In your app registration, click "Authentication" in the left menu

2. **Add Production URLs:**
   - Under "Single-page application" section
   - Click "Add URI"
   - Add your production login redirect URL (e.g., `https://yourapp.azurewebsites.net/auth`)
   - Add your production logout redirect URL (e.g., `https://yourapp.azurewebsites.net/logout`)
   - Add staging URLs as needed
   - Click "Save" at the bottom

3. **Configure Logout URLs:**
   - Scroll down to "Front-channel logout URL"
   - Add your logout URLs if different from redirect URIs

### Step 5: Test the Authentication

1. **Start your application:**
   ```bash
   npm start
   ```

2. **Test Sign-In:**
   - Navigate to `http://localhost:1234`
   - You should see the Login page with a "Sign in with Microsoft" button
   - Click the "Sign in with Microsoft" button
   - A popup window will appear with the Microsoft Sign-In page
   - Sign in with any Microsoft account
   - Grant consent for User.Read permission
   - You should be automatically redirected to the Dashboard

3. **Verify User Information:**
   - Check the header - you should see a "Logout" button
   - Check the sidebar footer - you should see your avatar, name, and email
   - When you collapse the sidebar, only the user icon should show

4. **Test Sign-Out:**
   - Click the "Logout" button in the header
   - You should be signed out and see the Logout confirmation page
   - The Logout page displays a "You've been logged out" message
   - You can click "Sign in again" to return to the login flow

## Configuration Files

- `.env` - Contains your Azure AD Client ID (git-ignored for security)
- `.env.example` - Template for environment variables
- `src/authConfig.ts` - MSAL configuration for Microsoft Sign-In
- `src/index.tsx` - Application entry point with MSAL initialization and routing logic
- `src/pages/Login.tsx` - Login page for unauthenticated users
- `src/pages/Logout.tsx` - Logout confirmation page
- `src/components/AuthButton.tsx` - Logout button component in header
- `src/components/AdminLayout.tsx` - Displays user info in sidebar footer

## How It Works

- **Authority**: `https://login.microsoftonline.com/consumers` - Personal Microsoft accounts only
- **Scopes**: `User.Read` - Access to basic user profile information
- **Prompt**: `select_account` - Account picker shown every time for easy account switching
- **Cache**: `localStorage` - Authentication tokens stored in browser local storage
- **Login Flow**: Popup-based authentication (no full page redirect)
- **Redirect URIs**: 
  - Login: `http://localhost:1234/auth` - Handles authentication callback
  - Logout: `http://localhost:1234/logout` - Shows logout confirmation page

## Application Flow

1. **Unauthenticated User**:
   - Visits `http://localhost:1234/`
   - Sees the Login page (no sidebar or header)
   - Clicks "Sign in with Microsoft"
   - Popup window opens with Microsoft account picker
   
2. **Authentication**:
   - User selects account and signs in
   - Grants consent for User.Read permission
   - Popup closes automatically
   - User is redirected to Dashboard

3. **Authenticated User**:
   - Sees full application with sidebar and header
   - User info displayed in sidebar footer
   - Can navigate to different pages
   - Can click "Logout" button in header

4. **Logout**:
   - User clicks "Logout" button
   - MSAL clears tokens and session
   - User redirected to `/logout` page
   - Sees confirmation message with option to sign in again

## Supported Account Types

✅ Personal Microsoft accounts (@outlook.com, @hotmail.com, @live.com)  
✅ Work or school accounts (Azure AD)  
✅ Guest accounts in Azure AD tenants

## Troubleshooting

### Issue: "AADSTS700016: Application not found in directory"
- **Solution**: Make sure your Client ID is correct in the `.env` file

### Issue: "Redirect URI mismatch"
- **Solution**: Verify that both `http://localhost:1234/auth` and `http://localhost:1234/logout` are registered as SPA redirect URIs in Azure Portal

### Issue: "Login button does nothing"
- **Solution**: Check browser console for errors. Make sure the .env file is in the correct location (`packages/shell/.env`)

### Issue: User info not showing
- **Solution**: Make sure you granted User.Read permission and consented during sign-in

## Security Best Practices

1. **Never commit your .env file** - It's already in .gitignore
2. **Use different Client IDs** for development, staging, and production
3. **Rotate your client secrets regularly** (if using confidential client)
4. **Review API permissions** - Only request what you need
5. **Enable logging** in production to track authentication issues

## Additional Resources

- [MSAL.js Documentation](https://github.com/AzureAD/microsoft-authentication-library-for-js)
- [Azure AD App Registration](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Microsoft Identity Platform](https://learn.microsoft.com/en-us/azure/active-directory/develop/)
