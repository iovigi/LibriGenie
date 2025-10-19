import { IsAuth } from "./services/auth/is-auth-service"

export async function middleware(request) {
  if (request.nextUrl.pathname.startsWith('/dashboard')) {
    const accessToken = request.cookies.get('accessToken');
    const refreshToken = request.cookies.get('refreshToken');
    
    // If no tokens at all, redirect to sign-in
    if (!accessToken?.value && !refreshToken?.value) {
      return Response.redirect(new URL('/auth/sign-in', request.url));
    }

    // Check access token first
    if (accessToken?.value) {
      const isAccessTokenValid = await IsAuth(accessToken.value);
      if (isAccessTokenValid) {
        return; // Access granted
      }
    }

    // If access token is invalid/expired, try refresh token
    if (refreshToken?.value) {
      const isRefreshTokenValid = await IsAuth(refreshToken.value);
      if (isRefreshTokenValid) {
        // Try to refresh the access token
        try {
          const refreshResponse = await fetch(new URL('/api/auth/refresh', request.url), {
            method: 'POST',
            headers: {
              'Cookie': request.headers.get('cookie') || '',
            },
          });
          
          if (refreshResponse.ok) {
            // Get the new cookies from the refresh response
            const setCookieHeader = refreshResponse.headers.get('set-cookie');
            if (setCookieHeader) {
              // Create a new response with the updated cookies
              const response = new Response(null, { status: 200 });
              response.headers.set('set-cookie', setCookieHeader);
              return response;
            }
          }
        } catch (error) {
          console.error('Token refresh failed:', error);
        }
      }
    }

    // If we get here, both tokens are invalid
    return Response.redirect(new URL('/auth/sign-in', request.url));
  }
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|.*\\.png$).*)'],
}