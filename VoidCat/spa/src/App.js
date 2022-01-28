import { Fragment } from 'react';
import { FilePreview } from "./FilePreview";
import { Dropzone } from "./Dropzone";
import { GlobalStats } from "./GlobalStats";

import './App.css';

function App() {
    let hasPath = window.location.pathname !== "/";
    return (
        <div className="app">
            {hasPath ? <FilePreview id={window.location.pathname.substr(1)} />
                : (
                    <Fragment>
                        <Dropzone />
                        <GlobalStats />
                    </Fragment>
                )}
        </div>
    );
}

export default App;
