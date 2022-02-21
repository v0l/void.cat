import QRCode from "qrcode.react";
import {Countdown} from "./Countdown";
import {useEffect} from "react";
import {PaywallOrderState} from "./Const";

export function LightningPaywall(props) {
    const file = props.file;
    const order = props.order;
    const onPaid = props.onPaid;
    const link = `lightning:${order.lnInvoice}`;

    function openInvoice() {
        let a = document.createElement("a");
        a.href = link;
        a.click();
    }

    async function checkStatus() {
        let req = await fetch(`/upload/${file.id}/paywall/${order.id}`);
        if (req.ok) {
            let order = await req.json();

            if (order.status === PaywallOrderState.Paid && typeof onPaid === "function") {
                onPaid(order);
            }
        }
    }

    useEffect(() => {
        let t = setInterval(checkStatus, 2500);
        return () => clearInterval(t);
    }, []);

    return (
        <div className="lightning-invoice" onClick={openInvoice}>
            <QRCode
                value={link}
                size={512}
                includeMargin={true}/>
            <dl>
                <dt>Expires:</dt>
                <dd><Countdown to={order.expire} onEnded={props.onReset}/></dd>
            </dl>
        </div>
    );
}