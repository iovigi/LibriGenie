import React from 'react';
import Head from 'next/head';
import Link from 'next/link';

export default function Custom404() {
  return (
    <>
      <Head>
        <title>404 - Page Not Found | Libri Genie</title>
      </Head>
      <div className="container-fluid d-flex align-items-center justify-content-center" style={{ minHeight: '100vh' }}>
        <div className="text-center">
          <div className="mb-4">
            <img src="/logo-home.png" alt="Libri Genie" width="100" height="80" />
          </div>
          <h1 className="display-1 text-primary">404</h1>
          <h2 className="mb-4">Page Not Found</h2>
          <p className="lead mb-4">
            The page you're looking for doesn't exist or has been moved.
          </p>
          <div className="d-flex gap-3 justify-content-center">
            <Link href="/" className="btn btn-primary">
              🏠 Go Home
            </Link>
            <Link href="/auth/sign-in" className="btn btn-outline-primary">
              🔑 Sign In
            </Link>
          </div>
        </div>
      </div>
    </>
  );
}
