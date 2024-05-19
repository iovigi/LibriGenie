import "bootstrap/dist/css/bootstrap.min.css"; // Import bootstrap CSS
import "../styles/global.css";
import { useEffect } from "react";
import Head from 'next/head';


function MyApp({ Component, pageProps }) {
    useEffect(() => {
        require("bootstrap/dist/js/bootstrap.bundle.min.js");
    }, []);

    return <Component {...pageProps} />;
}

export default MyApp;