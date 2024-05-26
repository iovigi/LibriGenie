import Head from 'next/head';
import styles from '../../styles/auth/Sign-In.module.css';
import { toast } from 'react-hot-toast';
import { FormEvent } from 'react'
import { useRouter } from 'next/router'

export default function SignIn() {
  const router = useRouter()
  const handleSubmit = async (event) => {
    event.preventDefault();

    const formData = new FormData(event.currentTarget)
    const email = formData.get('email')
    const password = formData.get('password')

    const response = await fetch('/api/auth/sign-in', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    })

    let result = await response.json();

    if (result.ok) {
      router.push('/dashboard')
    } else {
      toast.error(result.error);
    }
  };

  return (
    <main className={styles.main_signin}>
      <Head>
        <title>Libri Genie</title>
      </Head>
      <div className={styles.form_signin}>
        <form onSubmit={handleSubmit}>
          <div className='text-center'>
            <img className="mb-4 text-center" src="../logo-home.png" alt="" width="72" height="57" />
          </div>
          <h1 className="h3 mb-3 fw-normal">Please sign in</h1>

          <div className="form-floating">
            <input type="email" className="form-control" id="email" name="email" />
            <label htmlFor="email">Email address</label>
          </div>
          <div className="form-floating">
            <input type="password" className="form-control" id="password" name="password" />
            <label htmlFor="password">Password</label>
          </div>
          <button className="w-100 btn btn-lg btn-primary btn-primary-color" type="submit">Sign in</button>
        </form>
      </div>
    </main>
  );
}
