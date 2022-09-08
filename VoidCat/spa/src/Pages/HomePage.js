import {Dropzone} from "../Components/FileUpload/Dropzone";
import {GlobalStats} from "../Components/HomePage/GlobalStats";
import {FooterLinks} from "../Components/HomePage/FooterLinks";
import {MetricsGraph} from "../Components/HomePage/MetricsGraph";
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