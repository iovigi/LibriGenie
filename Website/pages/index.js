import Head from 'next/head';
import Link from 'next/link';

export default function Home() {
  return (
    <main>
      <Head>
        <title>Libri Genie</title>
      </Head>
      <div className="px-4 pt-5 text-center">
        <img className="d-block mx-auto mb-4" src="logo-home.png" alt="Logo" width="200" height="200" />
        <h1 className="display-4 fw-bold header-color">Libri Genie</h1>
        <div className="col-lg-6 mx-auto">
          <p className="lead mb-4">LibriGenie is an invaluable content generator designed to streamline your publishing schedule.</p>
          <div className="d-grid gap-2 d-sm-flex justify-content-sm-center mb-5">
            <Link href="/auth/sign-in" type="button" className="btn btn-primary btn-lg px-4 me-sm-3 btn-primary-color">Sign In</Link>
            <Link href="/auth/sign-up" type="button" className="btn btn-primary btn-lg px-4 btn-primary-color">Sign Up</Link>
          </div>
        </div>
        <div className="overflow-hidden img-home">
          <div className="container px-5">
            <img src="./genie.png" className="img-fluid border rounded-3 shadow-lg mb-4" alt="Genie" width="700" height="500" loading="lazy" />
          </div>
        </div>
      </div>
    </main>
  );
}
