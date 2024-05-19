import Head from 'next/head';
import styles from '../../styles/auth/Sign-Up.module.css';

export default function SignUp() {
  return (
    <main className={styles.main_signin}>
      <div className={styles.form_signin}>
        <form>
          <div className='text-center'>
            <img className="mb-4 text-center" src="../logo-home.png" alt="" width="72" height="57" />
          </div>
          <h1 className="h3 mb-3 fw-normal">Please sign up</h1>

          <div class="form-floating">
            <input type="email" className="form-control" id="floatingInput" placeholder="name@example.com" />
            <label for="floatingInput">Email address</label>
          </div>
          <div className="form-floating">
            <input type="password" class="form-control" id="floatingPassword" placeholder="Password" />
            <label for="floatingPassword">Password</label>
          </div>

          <div className="checkbox mb-3">
            <label>
              <input type="checkbox" value="remember-me" /> Remember me
            </label>
          </div>
          <button className="w-100 btn btn-lg btn-primary btn-primary-color" type="submit">Sign up</button>
        </form>
      </div>
    </main>
  );
}