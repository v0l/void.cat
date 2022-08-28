import {Dropzone} from "./Dropzone";
import {GlobalStats} from "./GlobalStats";
import {FooterLinks} from "./FooterLinks";
import {MetricsGraph} from "./MetricsGraph";
import {useSelector} from "react-redux";

export function HomePage() {
    const metrics = useSelector(a => a.info.info);
    return (
        <div className="page">
            <Dropzone/>
            <GlobalStats/>
            <MetricsGraph metrics={metrics}/>
            <FooterLinks/>
        </div>
    );
}