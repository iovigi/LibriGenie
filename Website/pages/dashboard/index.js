import Head from 'next/head';
import Link from 'next/link';
import styles from '../../styles/Dashboard/Dashboard.module.css';
import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/router';
import Cron from 'react-js-cron';
import 'react-js-cron/dist/styles.css';

import { toast } from 'react-hot-toast';


const Dashboard = function Dashboard() {
    const router = useRouter();
    const [currentTaskId, setCurrentTaskId] = useState('');
    const [tasks, setTasks] = useState([]);
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
    const [coinbaseName, setCoinbaseName] = useState('');
    const [coinbasePrivateKey, setCoinbasePrivateKey] = useState('');
    const [taskName, setTaskName] = useState('');
    const [eventBase, setEventBase] = useState(false);

    const handleLogout = async () => {
        try {
            const response = await fetch('/api/auth/logout', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
            });

            if (response.ok) {
                const result = await response.json();
                console.log('Logout successful:', result);
                router.push('/');
            } else {
                const error = await response.json();
                console.error('Logout failed:', error);
                toast.error("Logout failed: " + (error.error || 'Unknown error'));
            }
        } catch (error) {
            console.error('Logout error:', error);
            toast.error("Logout failed: " + error.message);
        }
    };

    const handleSubmit = async (event) => {
        event.preventDefault();

        const settingData = {
            name: taskName,
            category, 
            typeTrigger, 
            cron, 
            time, 
            enableWordpress, 
            urlWordpress, 
            usernameWordpress, 
            passwordWordpress, 
            enable, 
            symbols, 
            primarySymbols, 
            coinbaseName, 
            coinbasePrivateKey,
            eventBase
        };

        const response = await fetch('/api/dashboard/save-settings', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ 
                action: currentTaskId ? 'update' : 'add',
                settingId: currentTaskId,
                setting: settingData
            }),
        })

        if (response.ok) {
            toast.success("Successfully updated");
            loadTasks();
        }
        else {
            toast.error("Something went wrong");
        }
    }

    const loadTasks = async () => {
        try {
            const response = await fetch('/api/dashboard/get-settings', {
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
            });
            const data = await response.json();
            
            if (data.ok && data.settings) {
                setTasks(data.settings);
                if (data.settings.length > 0 && !currentTaskId) {
                    setCurrentTaskId(data.settings[0].id);
                    loadTaskData(data.settings[0]);
                }
            }
        } catch (error) {
            console.error('Error loading tasks:', error);
        }
    }

    const loadTaskData = (task) => {
        setTaskName(task.name || '');
        setCategory(task.category || '');
        setTime(task.time || '00:00');
        setEnableWordpress(task.enableWordpress || false);
        setUrlWordpress(task.urlWordpress || '');
        setUsernameWordpress(task.usernameWordpress || '');
        setPasswordWordpress(task.passwordWordpress || '');
        setEnable(task.enable || false);
        setTypeTrigger(task.typeTrigger || 0);
        setCron(task.cron || '');
        setSymbols(task.symbols || []);
        setPrimarySymbols(task.primarySymbols || []);
        setCoinbaseName(task.coinbaseName || '');
        setCoinbasePrivateKey(task.coinbasePrivateKey || '');
        setEventBase(task.eventBase || false);
    }

    const handleTaskSelect = (taskId) => {
        setCurrentTaskId(taskId);
        const selectedTask = tasks.find(task => task.id === taskId);
        if (selectedTask) {
            loadTaskData(selectedTask);
        }
    }

    const handleAddTask = () => {
        setCurrentTaskId('');
        setTaskName('');
        setCategory('');
        setTime('00:00');
        setEnableWordpress(false);
        setUrlWordpress('');
        setUsernameWordpress('');
        setPasswordWordpress('');
        setEnable(false);
        setTypeTrigger(0);
        setCron('');
        setSymbols([]);
        setPrimarySymbols([]);
        setCoinbaseName('');
        setCoinbasePrivateKey('');
        setEventBase(false);
    }

    const handleDeleteTask = async () => {
        if (!currentTaskId) return;
        
        if (confirm('Are you sure you want to delete this task?')) {
            try {
                const response = await fetch('/api/dashboard/save-settings', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify({ 
                        action: 'delete',
                        settingId: currentTaskId
                    }),
                });

                if (response.ok) {
                    toast.success("Task deleted successfully");
                    loadTasks();
                    if (tasks.length > 1) {
                        const remainingTasks = tasks.filter(task => task.id !== currentTaskId);
                        if (remainingTasks.length > 0) {
                            setCurrentTaskId(remainingTasks[0].id);
                            loadTaskData(remainingTasks[0]);
                        }
                    } else {
                        handleAddTask();
                    }
                } else {
                    toast.error("Failed to delete task");
                }
            } catch (error) {
                toast.error("Error deleting task");
            }
        }
    }

    useEffect(() => {
        loadTasks();
    }, [])

    // Fetch crypto symbols when Crypto Spikes category is selected
    useEffect(() => {
        if (category === 'CryptoSpikes' && availableSymbols.length === 0) {
            setLoadingSymbols(true);
            fetch('/api/crypto/crypto-symbols', {
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
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
            <div className="navbar-nav">
                <div className="nav-item text-nowrap">
                    <button 
                        className="btn btn-outline-light btn-sm" 
                        onClick={handleLogout}
                    >
                        Logout
                    </button>
                </div>
            </div>
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
                             {/* Task Management Section */}
                             <div className='row mt-3'>
                                 <div className='col-12'>
                                     <div className='d-flex justify-content-between align-items-center mb-3'>
                                         <div className='d-flex align-items-center gap-3'>
                                             <label className='form-label mb-0'>Task:</label>
                                             <select 
                                                 className='form-select' 
                                                 style={{width: '300px'}}
                                                 value={currentTaskId} 
                                                 onChange={(e) => handleTaskSelect(e.target.value)}
                                             >
                                                 <option value="">Select a task...</option>
                                                 {tasks.map(task => (
                                                     <option key={task.id} value={task.id}>
                                                         {task.name || `Task ${task.id.substring(0, 8)}`}
                                                     </option>
                                                 ))}
                                             </select>
                                         </div>
                                         <div className='d-flex gap-2'>
                                             <button 
                                                 type="button" 
                                                 className='btn btn-success btn-sm'
                                                 onClick={handleAddTask}
                                             >
                                                 Add Task
                                             </button>
                                             {currentTaskId && (
                                                 <button 
                                                     type="button" 
                                                     className='btn btn-danger btn-sm'
                                                     onClick={handleDeleteTask}
                                                 >
                                                     Delete Task
                                                 </button>
                                             )}
                                         </div>
                                     </div>
                                 </div>
                             </div>
                             
                             {/* Task Name */}
                             <div className='row mt-3'>
                                 <label className='form-label' htmlFor='taskName'>Task Name</label>
                                 <div className='col-12'>
                                     <input 
                                         type="text" 
                                         className='form-control' 
                                         value={taskName} 
                                         onChange={(e) => setTaskName(e.target.value)} 
                                         placeholder='Enter a name for this task'
                                         required
                                     />
                                 </div>
                             </div>
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
                             
                             <div className='row mt-3'>
                                 <div className='col-12'>
                                     <div className='form-check'>
                                         <input 
                                             name="event-base" 
                                             className='form-check-input' 
                                             type='checkbox' 
                                             checked={eventBase} 
                                             onChange={(e) => setEventBase(e.target.checked)} 
                                         />
                                         <label className='form-check-label' htmlFor='event-base'>Event Base</label>
                                     </div>
                                 </div>
                             </div>
                             
                             
                                                           {!eventBase && (
                                  <div className='row mt-3 col-12'>
                                      <label className='form-label' htmlFor='typeTrigger'>Trigger Type</label>
                                      <div className='col-12'>
                                          <select 
                                              className='form-select' 
                                              value={typeTrigger} 
                                              onChange={(e) => setTypeTrigger(parseInt(e.target.value))}
                                          >
                                              <option value="0">Time of the day to trigger</option>
                                              <option value="1">Cron Expression</option>
                                          </select>
                                      </div>
                                  </div>
                              )}
                                                         {!eventBase && typeTrigger == 0 && <div className='row mt-3 col-12'>
                                 <label className='form-label' htmlFor='time'>UTC Time of the day to trigger the generation</label>
                                 <div className='col-12'>
                                     <input required type="time" className='form-control' value={time} onChange={(e) => setTime(e.target.value)} />
                                 </div>
                             </div>}
                             {!eventBase && typeTrigger == 1 && <div className='row mt-3 col-12'>
                                 <label className='form-label' htmlFor='cron'>Cron Expression</label>
                                 <div className='col-12'>
                                     <div className="border rounded p-3 bg-light">
                                         <Cron 
                                             value={cron} 
                                             setValue={setCron}
                                             defaultPeriod="day"
                                             humanizeLabels={true}
                                             clearButton={true}
                                         />
                                     </div>
                                     {cron && (
                                         <div className="mt-2">
                                             <small className="text-muted">
                                                 <strong>Expression:</strong> <code>{cron}</code>
                                             </small>
                                         </div>
                                     )}
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
                             
                             {category === 'CryptoSpikes' && (
                                 <>
                                     <div className='row mt-3'>
                                         <label className='form-label' htmlFor='coinbase-name'>Coinbase Account Name</label>
                                         <div className='col-12'>
                                             <input 
                                                 type="text" 
                                                 className='form-control' 
                                                 value={coinbaseName} 
                                                 onChange={(e) => setCoinbaseName(e.target.value)} 
                                                 placeholder='Enter your Coinbase account name' 
                                             />
                                         </div>
                                     </div>
                                     <div className='row mt-3'>
                                         <label className='form-label' htmlFor='coinbase-private-key'>Coinbase Private Key</label>
                                         <div className='col-12'>
                                             <input 
                                                 type="password" 
                                                 className='form-control' 
                                                 value={coinbasePrivateKey} 
                                                 onChange={(e) => setCoinbasePrivateKey(e.target.value)} 
                                                 placeholder='Enter your Coinbase private key' 
                                             />
                                             <small className="form-text text-muted">
                                                 This will be used for enhanced crypto monitoring and trading features.
                                             </small>
                                         </div>
                                     </div>
                                 </>
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
