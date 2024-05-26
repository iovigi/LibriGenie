import Head from 'next/head';
import Link from 'next/link';
import styles from '../../styles/Dashboard/Dashboard.module.css';
import React, { useState } from 'react';
import 'react-time-picker/dist/TimePicker.css';
import 'react-clock/dist/Clock.css';
import TimePicker from 'react-time-picker';


const Dashboard = function Dashboard() {
    const [email, setEmail] = useState('');
    const [category, setCategory] = useState('');
    const [time, setTime] = useState('00:00');
    const [enableWordpress, setEnableWordpress] = useState(false);
    const [usernameWordpress, setUsernameWordpress] = useState('');
    const [passwordWordpress, setPasswordWordpress] = useState('');

    return (<>
        <Head>
            <title>Libri Genie</title>
        </Head>
        <header className="navbar sticky-top bg-dark flex-md-nowrap p-0 shadow" data-bs-theme="dark">
            <a className="navbar-brand col-md-3 col-lg-2 me-0 px-3 fs-6 text-white" href="#">Libri Genie</a>
        </header>

        <div className="container-fluid">
            <div className="row">
                <div className="sidebar border border-right col-md-3 col-lg-2 p-0 bg-body-tertiary">
                    <div className="offcanvas-md offcanvas-end bg-body-tertiary" id="sidebarMenu" aria-labelledby="sidebarMenuLabel">
                        <div className="offcanvas-header">
                            <h5 className="offcanvas-title" id="sidebarMenuLabel">Libri Genie</h5>
                            <button type="button" className="btn-close" data-bs-dismiss="offcanvas" data-bs-target="#sidebarMenu" aria-label="Close"></button>
                        </div>
                        <div className="offcanvas-body d-md-flex flex-column p-0 pt-lg-3 overflow-y-auto">
                            <ul className={`${styles.sidebar_menu} nav flex-column`}>
                                <li className="nav-item">
                                    <a className="nav-link d-flex align-items-center gap-2 active" aria-current="page" href="#">
                                        Settings
                                    </a>
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>

                <main className="col-md-9 ms-sm-auto col-lg-10 px-md-4">
                    <div className="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom">
                        <h1 className="h2">Settings</h1>
                    </div>
                    <div className='container-fluid'>
                        <div className='row'>
                            <label className='form-label' htmlFor='email'>Email</label>
                            <div className='col-3'> <input type="text" className='form-control' placeholder='Enter email' value={email} onChange={(e) => setEmail(e.target.value)} /></div>
                        </div>
                        <div className='row mt-3'>
                            <label className='form-label' htmlFor='time'>Time of the day</label>
                            <div className='col-3'>
                                <TimePicker className="form-control" amPmAriaLabel="Select AM/PM"
                                    clearAriaLabel="Clear value"
                                    clockAriaLabel="Toggle clock"
                                    hourAriaLabel="Hour"
                                    maxDetail="minute"
                                    minuteAriaLabel="Minute"
                                    nativeInputAriaLabel="Time" onChange={setTime} value={time} />
                            </div>
                        </div>
                        <div className='row mt-3'>
                            <div className='col-3'>
                                <div className='form-check'>
                                    <input name="enable-wordpress" className='form-check-input' type='checkbox' value={enableWordpress} onChange={(e) => setEnableWordpress(e.target.checked)} />
                                    <label className='form-check-label' htmlFor='enable-wordpress'>Enable wordpress integration</label>
                                </div>
                            </div>
                        </div>
                        {enableWordpress && <div className='row mt-3'>
                            <label className='form-label' htmlFor='wordpress-username'>Wordpress Api Username</label>
                            <div className='col-3'><input type="text" value={usernameWordpress} onChange={(e) => setUsernameWordpress(e.target.value)} className='form-control' placeholder='Enter wordpress api username' /></div>
                        </div>}
                        {enableWordpress && <div className='row mt-3'>
                            <label className='form-label' htmlFor='wordpress-password'>Wordpress API Password</label>
                            <div className='col-3'><input type="password" value={passwordWordpress} onChange={(e) => setPasswordWordpress(e.target.value)} className='form-control' placeholder='Enter wordpress api password' /></div>
                        </div>}
                        <div className='row mt-3'>
                            <label className='form-label' htmlFor='category'>Category</label>
                            <div className='col-3'> <select className='form-select' value={category} onChange={(e) => setCategory(e.target.value)}>
                                <option disabled value="">Please select category</option>
                                <option value="Joke">Joke</option>
                                <option value="Poem">Poem</option>
                                <option value="Summarize">Summarize News</option>
                            </select></div>
                        </div>
                        {category == "Summarize" &&
                            <div className='row mt-3'>
                                <label className='form-label' htmlFor='rss'>Rss</label>
                                <div className='col-3'><input type="text" className='form-control' placeholder='Enter rss' /></div>
                            </div>}
                        <div className='row mt-3'>
                            <div className='col-3'>
                                <button className='btn btn-primary w-100'>Save</button>
                            </div>
                        </div>
                    </div>
                </main>
            </div>
        </div>
    </>
    );
}

export default Dashboard;
