import {useState} from "react";
import FeatherIcon from "feather-icons-react";
import {PaymentCurrencies} from "./Const";
import {VoidButton} from "./VoidButton";

export function StrikePaymentConfig(props) {
    const file = props.file;
    const privateFile = props.privateFile;
    const onSaveConfig = props.onSaveConfig;
    const payment = file.payment;
    const editSecret = privateFile.metadata.editSecret;

    const [username, setUsername] = useState(payment?.handle ?? "hrf");
    const [currency, setCurrency] = useState(payment?.cost.currency ?? PaymentCurrencies.USD);
    const [price, setPrice] = useState(payment?.cost.amount ?? 1);
    const [saveStatus, setSaveStatus] = useState();

    async function saveStrikeConfig(e) {
        let cfg = {
            editSecret,
            strike: {
                handle: username,
                cost: {
                    currency: currency,
                    amount: price
                }
            }
        };

        if (typeof onSaveConfig === "function") {
            if (await onSaveConfig(cfg)) {
                setSaveStatus(true);
            } else {
                alert("Error settings payment config!");
                setSaveStatus(false);
            }
        }
    }

    return (
        <div>
            <dl>
                <dt>Stike username:</dt>
                <dd><input type="text" value={username} onChange={(e) => setUsername(e.target.value)}/></dd>
                <dt>Currency:</dt>
                <dd>
                    <select onChange={(e) => setCurrency(parseInt(e.target.value))} value={currency}>
                        <option value={PaymentCurrencies.BTC}>BTC</option>
                        <option value={PaymentCurrencies.USD}>USD</option>
                        <option value={PaymentCurrencies.EUR}>EUR</option>
                        <option value={PaymentCurrencies.GBP}>GBP</option>
                    </select>
                </dd>
                <dt>Price:</dt>
                <dd><input type="number" value={price} onChange={(e) => setPrice(parseFloat(e.target.value))}/></dd>
            </dl>
            <VoidButton onClick={saveStrikeConfig}>Save</VoidButton>
            {saveStatus ? <FeatherIcon icon="check-circle"/> : null}
        </div>
    );
}