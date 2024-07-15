import Head from 'next/head';
import Link from 'next/link';
import styles from '../../styles/Dashboard/Dashboard.module.css';
import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/router';

import { toast } from 'react-hot-toast';


const Dashboard = function Dashboard() {
    const [typeTrigger, setTypeTrigger] = useState(0);
    const [category, setCategory] = useState('');
    const [cron, setCron] = useState('');
    const [time, setTime] = useState('00:00');
    const [enableWordpress, setEnableWordpress] = useState(false);
    const [urlWordpress, setUrlWordpress] = useState('');
    const [usernameWordpress, setUsernameWordpress] = useState('');
    const [passwordWordpress, setPasswordWordpress] = useState('');
    const [enable, setEnable] = useState(false);

    const handleSubmit = async (event) => {
        event.preventDefault();

        const response = await fetch('/api/dashboard/save-settings', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: "same-origin",
            body: JSON.stringify({ category, typeTrigger, cron, time, enableWordpress, urlWordpress, usernameWordpress, passwordWordpress, enable }),
        })

        if (response.ok) {
            toast.success("Successfully updated");
        }
        else {
            toast.error("Something went wrong");
        }
    }

    useEffect(() => {
        fetch('/api/dashboard/get-settings', {
            headers: { 'Content-Type': 'application/json' },
            credentials: "same-origin",
        })
            .then((res) => res.json())
            .then((data) => {
                if (!data.settings) {
                    return;
                }

                setCategory(data.settings?.category);
                setTime(data.settings?.time);
                setEnableWordpress(data.settings?.enableWordpress);
                setUrlWordpress(data.settings?.urlWordpress);
                setUsernameWordpress(data.settings?.usernameWordpress);
                setPasswordWordpress(data.settings?.passwordWordpress);
                setEnable(data.settings?.enable);
                setTypeTrigger(data.settings?.typeTrigger);
                setCron(data.settings?.cron);
            })
    }, [])

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
                    <div className='container d-flex justify-content-center align-items-center'>
                        <form className='row w-75' onSubmit={handleSubmit}>
                            <div className='row mt-3 col-12'>
                                <label className='form-label' htmlFor='time'>Cron Expression or time of the day to trigger the generation</label>
                                <div className='col-12'>
                                    <div class="form-check">
                                        <input type="radio" name="type-trigger" value="0" defaultChecked checked={typeTrigger == 0} onChange={(e) => setTypeTrigger(e.target.value)} className='form-check-input' />
                                        <label class="form-check-label" for="0">
                                            Time of the day to trigger
                                        </label>
                                    </div>
                                    <div class="form-check">
                                        <input type="radio" name="type-trigger" value="1" checked={typeTrigger == 1} onChange={(e) => setTypeTrigger(e.target.value)} className='form-check-input' />
                                        <label class="form-check-label" for="1">
                                            Cron
                                        </label>
                                    </div>
                                </div>
                            </div>
                            {typeTrigger == 0 && <div className='row mt-3 col-12'>
                                <label className='form-label' htmlFor='time'>UTC Time of the day to trigger the generation</label>
                                <div className='col-12'>
                                    <input required type="time" className='form-control' value={time} onChange={(e) => setTime(e.target.value)} />
                                </div>
                            </div>}
                            {typeTrigger == 1 && <div className='row mt-3 col-12'>
                                <label className='form-label' htmlFor='cron'>Cron Expression</label>
                                <div className='col-12'>
                                    <input required type="input" className='form-control' value={cron} onChange={(e) => setCron(e.target.value)} />
                                </div>
                            </div>}
                            <div className='row mt-3'>
                                <div className='col-12'>
                                    <div className='form-check'>
                                        <input name="enable-wordpress" className='form-check-input' type='checkbox' checked={enableWordpress} onChange={(e) => setEnableWordpress(e.target.checked)} />
                                        <label className='form-check-label' htmlFor='enable-wordpress'>Enable wordpress integration</label>
                                    </div>
                                </div>
                            </div>
                            {enableWordpress && <div className='row col-12 mt-3'>
                                <label className='form-label' htmlFor='wordpress-username'>Wordpress Url</label>
                                <div className='col-12'><input type="url" required value={urlWordpress} onChange={(e) => setUrlWordpress(e.target.value)} className='form-control' placeholder='wordpress Url' /></div>
                            </div>}
                            {enableWordpress && <div className='row col-12 mt-3'>
                                <label className='form-label' htmlFor='wordpress-username'>Wordpress Api Username</label>
                                <div className='col-12'><input type="text" required value={usernameWordpress} onChange={(e) => setUsernameWordpress(e.target.value)} className='form-control' placeholder='wordpress api username' /></div>
                            </div>}
                            {enableWordpress && <div className='row  col-12 mt-3'>
                                <label className='form-label' htmlFor='wordpress-password'>Wordpress API Password</label>
                                <div className='col-12'><input type="password" required value={passwordWordpress} onChange={(e) => setPasswordWordpress(e.target.value)} className='form-control' placeholder='wordpress api password' /></div>
                            </div>}
                            <div className='row mt-3'>
                                <label className='form-label' htmlFor='category'>Category</label>
                                <div className='col-12'> <select className='form-select' value={category} required onChange={(e) => setCategory(e.target.value)}>
                                    <option disabled value="">Please select category</option>
                                    <option value="Joke">Joke</option>
                                    <option value="Poem">Poem</option>
                                    <option value="Summarize">Summarize News</option>
                                </select></div>
                            </div>
                            <div className='row mt-3'>
                                <div className='col-12'>
                                    <div className='form-check'>
                                        <input name="enable" className='form-check-input' type='checkbox' checked={enable} onChange={(e) => setEnable(e.target.checked)} />
                                        <label className='form-check-label' htmlFor='enable'>Enable</label>
                                    </div>
                                </div>
                            </div>
                            <div className='row mt-3'>
                                <div>
                                    <button type='submit' className='btn btn-primary w-100'>Save</button>
                                </div>
                            </div>
                        </form>
                    </div>
                </main>
            </div>
        </div>
    </>
    );
}

export default Dashboard;
