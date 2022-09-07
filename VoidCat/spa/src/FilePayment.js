import "./FilePayment.css";
import {FormatCurrency} from "./Util";
import {PaymentServices} from "./Const";
import {useState} from "react";
import {LightningPayment} from "./LightningPayment";
import {useApi} from "./Api";
import {VoidButton} from "./VoidButton";

export function FilePayment(props) {
    const {Api} = useApi();
    const file = props.file;
    const pw = file.payment;
    const paymentKey = `payment-${file.id}`;
    const onPaid = props.onPaid;

    const [order, setOrder] = useState();

    async function fetchOrder() {
        let req = await Api.createOrder(file.id);
        if (req.ok && req.status === 200) {
            setOrder(await req.json());
        }
    }

    function reset() {
        setOrder(undefined);
    }

    function handlePaid(order) {
        window.localStorage.setItem(paymentKey, JSON.stringify(order));
        if (typeof onPaid === "function") {
            onPaid();
        }
    }

    if (!order) {
        return (
            <div className="payment">
                <h3>
                    You must pay {FormatCurrency(pw.cost.amount, pw.cost.currency)} to view this file.
                </h3>
                <VoidButton onClick={fetchOrder}>Pay</VoidButton>
            </div>
        );
    } else {
        switch (pw.service) {
            case PaymentServices.Strike: {
                return <LightningPayment file={file} order={order} onReset={reset} onPaid={handlePaid}/>;
            }
        }
        return null;
    }
}