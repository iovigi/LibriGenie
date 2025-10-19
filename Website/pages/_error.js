import React from 'react';
import Head from 'next/head';
import Link from 'next/link';

function Error({ statusCode }) {
  return (
    <>
      <Head>
        <title>Error {statusCode} - Libri Genie</title>
      </Head>
      <div className="container-fluid d-flex align-items-center justify-content-center" style={{ minHeight: '100vh' }}>
        <div className="text-center">
          <div className="mb-4">
            <img src="/logo-home.png" alt="Libri Genie" width="100" height="80" />
          </div>
          <h1 className="display-1 text-primary">{statusCode || 'Error'}</h1>
          <h2 className="mb-4">
            {statusCode === 404 
              ? 'Page Not Found' 
              : statusCode === 500 
              ? 'Internal Server Error' 
              : 'Something went wrong'
            }
          </h2>
          <p className="lead mb-4">
            {statusCode === 404 
              ? "The page you're looking for doesn't exist."
              : statusCode === 500
              ? "We're experiencing some technical difficulties."
              : "An unexpected error occurred."
            }
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

Error.getInitialProps = ({ res, err }) => {
  const statusCode = res ? res.statusCode : err ? err.statusCode : 404;
  return { statusCode };
};

export default Error;
