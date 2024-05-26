import { IsAuth } from "./services/auth/is-auth-service"

export async function middleware(request) {

  if (request.nextUrl.pathname.startsWith('/dashboard')) {
    const currentUser = request.cookies.get('token')
    console.log({currentUser})
    if (!currentUser && !currentUser?.value) {
      return Response.redirect(new URL('/auth/sign-in', request.url))
    }

    if (!IsAuth(currentUser.value)) {
      return Response.redirect(new URL('/auth/sign-in', request.url))
    }
  }
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|.*\\.png$).*)'],
}