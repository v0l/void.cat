import QRCode from "qrcode.react";
import {useEffect} from "react";

import {Countdown} from "../Shared/Countdown";
import {PaymentOrderState} from "../Shared/Const";
import {useApi} from "../Shared/Api";

export function LightningPayment(props) {
    const {Api} = useApi();
    const file = props.file;
    const order = props.order;
    const onPaid = props.onPaid;
    const link = `lightning:${order.invoice}`;

    function openInvoice() {
        let a = document.createElement("a");
        a.href = link;
        a.click();
    }

    async function checkStatus() {
        let req = await Api.getOrder(file.id, order.id);
        if (req.ok) {
            let order = await req.json();

            if (order.status === PaymentOrderState.Paid && typeof onPaid === "function") {
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
            <h1>Pay with Lightning ⚡</h1>
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