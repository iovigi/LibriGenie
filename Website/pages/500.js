import React from 'react';
import Head from 'next/head';
import Link from 'next/link';

export default function Custom500() {
  return (
    <>
      <Head>
        <title>500 - Server Error | Libri Genie</title>
      </Head>
      <div className="container-fluid d-flex align-items-center justify-content-center" style={{ minHeight: '100vh' }}>
        <div className="text-center">
          <div className="mb-4">
            <img src="/logo-home.png" alt="Libri Genie" width="100" height="80" />
          </div>
          <h1 className="display-1 text-danger">500</h1>
          <h2 className="mb-4">Internal Server Error</h2>
          <p className="lead mb-4">
            We're experiencing some technical difficulties. Please try again later.
          </p>
          <div className="d-flex gap-3 justify-content-center">
            <Link href="/" className="btn btn-primary">
              🏠 Go Home
            </Link>
            <button 
              className="btn btn-outline-primary" 
              onClick={() => window.location.reload()}
            >
              🔄 Try Again
            </button>
          </div>
        </div>
      </div>
    </>
  );
}
