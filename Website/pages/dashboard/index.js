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
    const [symbols, setSymbols] = useState([]);
    const [primarySymbols, setPrimarySymbols] = useState([]);
    const [availableSymbols, setAvailableSymbols] = useState([]);
    const [loadingSymbols, setLoadingSymbols] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');

    const handleSubmit = async (event) => {
        event.preventDefault();

        const response = await fetch('/api/dashboard/save-settings', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: "same-origin",
            body: JSON.stringify({ category, typeTrigger, cron, time, enableWordpress, urlWordpress, usernameWordpress, passwordWordpress, enable, symbols, primarySymbols }),
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
                setSymbols(data.settings?.symbols || []);
                setPrimarySymbols(data.settings?.primarySymbols || []);
            })
    }, [])

    // Fetch crypto symbols when Crypto Spikes category is selected
    useEffect(() => {
        if (category === 'CryptoSpikes' && availableSymbols.length === 0) {
            setLoadingSymbols(true);
            fetch('/api/crypto/crypto-symbols', {
                headers: { 'Content-Type': 'application/json' },
                credentials: "same-origin",
            })
                .then((res) => res.json())
                .then((data) => {
                    if (data.success) {
                        setAvailableSymbols(data.symbols);
                    }
                })
                .catch((error) => {
                    console.error('Error fetching symbols:', error);
                    toast.error("Failed to fetch crypto symbols");
                })
                .finally(() => {
                    setLoadingSymbols(false);
                });
        }
    }, [category, availableSymbols.length]);

    const handleSymbolToggle = (symbolId) => {
        setSymbols(prev => {
            if (prev.includes(symbolId)) {
                return prev.filter(id => id !== symbolId);
            } else {
                return [...prev, symbolId];
            }
        });
    };

    const handlePrimarySymbolToggle = (symbolId) => {
        setPrimarySymbols(prev => {
            if (prev.includes(symbolId)) {
                return prev.filter(id => id !== symbolId);
            } else {
                return [...prev, symbolId];
            }
        });
    };

    const handleSelectAll = () => {
        if (symbols.length === availableSymbols.length) {
            // If all are selected, deselect all
            setSymbols([]);
            setPrimarySymbols([]);
        } else {
            // If not all are selected, select all
            setSymbols(availableSymbols.map(symbol => symbol.id));
        }
    };

    const handleSelectAllPrimary = () => {
        // Get all enabled symbols
        const enabledSymbols = availableSymbols.filter(symbol => symbols.includes(symbol.id));
        
        // Check if all enabled symbols are already primary
        const allEnabledArePrimary = enabledSymbols.every(symbol => primarySymbols.includes(symbol.id));
        
        if (allEnabledArePrimary) {
            // If all enabled symbols are primary, deselect all primary
            setPrimarySymbols([]);
        } else {
            // If not all enabled symbols are primary, select all enabled symbols as primary
            const enabledSymbolIds = enabledSymbols.map(symbol => symbol.id);
            setPrimarySymbols(enabledSymbolIds);
        }
    };

    const isAllSelected = availableSymbols.length > 0 && symbols.length === availableSymbols.length;

    // Check if all enabled symbols are primary
    const enabledSymbols = availableSymbols.filter(symbol => symbols.includes(symbol.id));
    const isAllEnabledPrimary = enabledSymbols.length > 0 && enabledSymbols.every(symbol => primarySymbols.includes(symbol.id));

    // Filter symbols based on search term
    const filteredSymbols = availableSymbols.filter(symbol => {
        if (!searchTerm) return true;
        
        const searchLower = searchTerm.toLowerCase();
        const displayName = symbol.display_name?.toLowerCase() || '';
        const baseCurrency = symbol.base_currency?.toLowerCase() || '';
        const quoteCurrency = symbol.quote_currency?.toLowerCase() || '';
        const symbolId = symbol.id?.toLowerCase() || '';
        
        return displayName.includes(searchLower) || 
               baseCurrency.includes(searchLower) || 
               quoteCurrency.includes(searchLower) || 
               symbolId.includes(searchLower);
    });

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
                             <div className='row mt-3'>
                                 <label className='form-label' htmlFor='category'>Category</label>
                                 <div className='col-12'> <select className='form-select' value={category} required onChange={(e) => setCategory(e.target.value)}>
                                     <option disabled value="">Please select category</option>
                                     <option value="Joke">Joke</option>
                                     <option value="Poem">Poem</option>
                                     <option value="Summarize">Summarize News</option>
                                     <option value="CryptoSpikes">Crypto Spikes</option>
                                 </select></div>
                             </div>
                             
                             
                                                           {category !== 'CryptoSpikes' && (
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
                              )}
                                                         {category !== 'CryptoSpikes' && typeTrigger == 0 && <div className='row mt-3 col-12'>
                                 <label className='form-label' htmlFor='time'>UTC Time of the day to trigger the generation</label>
                                 <div className='col-12'>
                                     <input required type="time" className='form-control' value={time} onChange={(e) => setTime(e.target.value)} />
                                 </div>
                             </div>}
                             {category !== 'CryptoSpikes' && typeTrigger == 1 && <div className='row mt-3 col-12'>
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
                             
                             {category === 'CryptoSpikes' && (
                                 <div className='row mt-3'>
                                     <label className='form-label'>Select Crypto Symbols</label>
                                     <div className='col-12'>
                                         {loadingSymbols ? (
                                             <div className="text-center">
                                                 <div className="spinner-border" role="status">
                                                     <span className="visually-hidden">Loading...</span>
                                                 </div>
                                                 <p className="mt-2">Loading crypto symbols...</p>
                                             </div>
                                         ) : (
                                             <div className="row">
                                                                                                   {availableSymbols.length > 0 && (
                                                      <div className="col-12 mb-3">
                                                          <div className="d-flex justify-content-between align-items-center">
                                                              <div className="d-flex align-items-center">
                                                                  <button
                                                                      type="button"
                                                                      className="btn btn-outline-primary btn-sm"
                                                                      onClick={handleSelectAll}
                                                                  >
                                                                      {isAllSelected ? 'Deselect All' : 'Select All'}
                                                                  </button>
                                                                  <button
                                                                      type="button"
                                                                      className="btn btn-outline-primary btn-sm"
                                                                      onClick={handleSelectAllPrimary}
                                                                      style={{ marginLeft: '5px' }}
                                                                  >
                                                                      {isAllEnabledPrimary ? 'Deselect All Primary' : 'Select All Primary'}
                                                                  </button>
                                                              </div>
                                                              <div className="d-flex align-items-center">
                                                                  <label htmlFor="symbol-search" className="form-label me-2 mb-0">Search:</label>
                                                                  <input
                                                                      type="text"
                                                                      id="symbol-search"
                                                                      className="form-control form-control-sm"
                                                                      style={{ width: '200px' }}
                                                                      placeholder="Search symbols..."
                                                                      value={searchTerm}
                                                                      onChange={(e) => setSearchTerm(e.target.value)}
                                                                  />
                                                              </div>
                                                          </div>
                                                      </div>
                                                  )}
                                                 <div className="col-12">
                                                     <div className="border rounded p-3" style={{ height: '65vh', overflowY: 'auto' }}>
                                                         {filteredSymbols.map((symbol) => (
                                                             <div key={symbol.id} className="d-flex align-items-center justify-content-between py-2 border-bottom">
                                                                 <div className="d-flex align-items-center">
                                                                     <div className="form-check me-4">
                                                                         <input
                                                                             className="form-check-input"
                                                                             type="checkbox"
                                                                             id={`enable-${symbol.id}`}
                                                                             checked={symbols.includes(symbol.id)}
                                                                             onChange={() => handleSymbolToggle(symbol.id)}
                                                                         />
                                                                         <label className="form-check-label fw-bold" htmlFor={`enable-${symbol.id}`}>
                                                                             Enable
                                                                         </label>
                                                                     </div>
                                                                     <div className="form-check me-4">
                                                                         <input
                                                                             className="form-check-input"
                                                                             type="checkbox"
                                                                             id={`primary-${symbol.id}`}
                                                                             checked={primarySymbols.includes(symbol.id)}
                                                                             onChange={() => handlePrimarySymbolToggle(symbol.id)}
                                                                             disabled={!symbols.includes(symbol.id)}
                                                                         />
                                                                         <label className={`form-check-label ${!symbols.includes(symbol.id) ? 'text-muted' : 'fw-bold'}`} htmlFor={`primary-${symbol.id}`}>
                                                                             Primary
                                                                         </label>
                                                                     </div>
                                                                 </div>
                                                                 <div className="text-end">
                                                                     <span className="fw-semibold">{symbol.display_name}</span>
                                                                     <br />
                                                                     <small className="text-muted">{symbol.base_currency}/{symbol.quote_currency}</small>
                                                                 </div>
                                                             </div>
                                                         ))}
                                                         {filteredSymbols.length === 0 && (
                                                             <div className="text-center py-4">
                                                                 <p className="text-muted">
                                                                     {searchTerm ? `No symbols found matching "${searchTerm}"` : 'No symbols available'}
                                                                 </p>
                                                             </div>
                                                         )}
                                                     </div>
                                                 </div>
                                                 {availableSymbols.length === 0 && !loadingSymbols && (
                                                     <div className="col-12">
                                                         <p className="text-muted">No symbols available</p>
                                                     </div>
                                                 )}
                                             </div>
                                         )}
                                     </div>
                                 </div>
                             )}
                             
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
