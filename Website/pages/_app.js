import "bootstrap/dist/css/bootstrap.min.css"; // Import bootstrap CSS
import "../styles/global.css";
import { useEffect } from "react";
import { Toaster } from "react-hot-toast";


function MyApp({ Component, pageProps }) {
    useEffect(() => {
        require("bootstrap/dist/js/bootstrap.bundle.min.js");
    }, []);

    return <> 
        <Component {...pageProps} />
        <Toaster position="bottom-right" />
    </>;
}

export default MyApp;