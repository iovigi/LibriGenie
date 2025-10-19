import Head from 'next/head';
import styles from '../../styles/auth/Sign-Up.module.css';
import { toast } from 'react-hot-toast';
import { FormEvent, useEffect, useState } from 'react'
import { useRouter } from 'next/router'

export default function SignUp() {
  const router = useRouter()
  const [csrfToken, setCsrfToken] = useState('')

  useEffect(() => {
    // Fetch CSRF token on component mount
    const fetchCSRFToken = async (retryCount = 0) => {
      try {
        console.log(`Fetching CSRF token (attempt ${retryCount + 1})`);
        const response = await fetch('/api/auth/csrf-token', {
          method: 'GET',
          credentials: 'include' // Important: include cookies
        });
        
        if (response.ok) {
          const data = await response.json();
          if (data.ok) {
            setCsrfToken(data.csrfToken);
            console.log('CSRF token fetched successfully:', data.csrfToken.substring(0, 8) + '...');
          } else {
            console.error('Failed to get CSRF token:', data.error);
            if (retryCount < 2) {
              setTimeout(() => fetchCSRFToken(retryCount + 1), 1000);
            }
          }
        } else {
          console.error('CSRF token request failed:', response.status);
          if (retryCount < 2) {
            setTimeout(() => fetchCSRFToken(retryCount + 1), 1000);
          }
        }
      } catch (err) {
        console.error('Failed to fetch CSRF token:', err);
        if (retryCount < 2) {
          setTimeout(() => fetchCSRFToken(retryCount + 1), 1000);
        }
      }
    };

    fetchCSRFToken();
  }, [])

  const handleSubmit = async (event) => {
    event.preventDefault();

    // Check if CSRF token is available
    if (!csrfToken) {
      toast.error('CSRF token not loaded. Please refresh the page and try again.');
      return;
    }

    const formData = new FormData(event.currentTarget)
    const email = formData.get('email')
    const password = formData.get('password')

    console.log('Submitting sign-up with CSRF token:', csrfToken.substring(0, 8) + '...');

    try {
      const response = await fetch('/api/auth/sign-up', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include', // Important: include cookies
        body: JSON.stringify({ email, password, csrfToken }),
      })

      let result = await response.json();

      if (result.ok) {
        router.push('/dashboard')
      } else if (result.error && result.error.includes('CSRF token')) {
        // Try to get a fresh CSRF token
        console.log('CSRF token invalid, getting fresh token...');
        try {
          const freshResponse = await fetch('/api/auth/csrf-token', {
            method: 'GET',
            credentials: 'include'
          });
          
          if (freshResponse.ok) {
            const freshData = await freshResponse.json();
            if (freshData.ok) {
              setCsrfToken(freshData.csrfToken);
              toast.error('Please try signing up again with the fresh token.');
              return;
            }
          }
        } catch (refreshError) {
          console.error('Failed to get fresh CSRF token:', refreshError);
        }
        toast.error(result.error);
      } else {
        console.error('Sign-up error:', result.error);
        toast.error(result.error);
      }
    } catch (error) {
      console.error('Sign-up request failed:', error);
      toast.error('Sign-up request failed. Please try again.');
    }
  };

  return (
    <main className={styles.main_signin}>
      <Head>
        <title>Libri Genie</title>
      </Head>
      <div className={styles.form_signin}>
        <form  onSubmit={handleSubmit}>
          <div className='text-center'>
            <img className="mb-4 text-center" src="../logo-home.png" alt="" width="72" height="57" />
          </div>
          <h1 className="h3 mb-3 fw-normal">Please sign up</h1>

          <div className="form-floating">
            <input type="email" className="form-control" id="email" name="email" />
            <label htmlFor="email">Email address</label>
          </div>
          <div className="form-floating">
            <input type="password" className="form-control" id="password" name="password" />
            <label htmlFor="password">Password</label>
          </div>
          <button 
            className="w-100 btn btn-lg btn-primary btn-primary-color" 
            type="submit"
            disabled={!csrfToken}
          >
            {csrfToken ? 'Sign up' : 'Loading...'}
          </button>
          {!csrfToken && (
            <div className="mt-2 text-center">
              <small className="text-muted">Loading security token...</small>
            </div>
          )}
        </form>
      </div>
    </main>
  );
}