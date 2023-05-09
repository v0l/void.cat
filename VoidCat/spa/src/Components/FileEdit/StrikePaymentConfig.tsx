import {useState} from "react";
import {VoidButton} from "../Shared/VoidButton";

import {PaymentCurrencies, SetPaymentConfigRequest, VoidFileResponse} from "Api";

interface StrikePaymentConfigProps {
    file: VoidFileResponse
    privateFile: VoidFileResponse
    onSaveConfig: (cfg: SetPaymentConfigRequest) => Promise<any>
}

export function StrikePaymentConfig({file, privateFile, onSaveConfig}: StrikePaymentConfigProps) {
    const payment = file.payment;
    const editSecret = privateFile.metadata!.editSecret;

    const [username, setUsername] = useState(payment?.strikeHandle ?? "hrf");
    const [currency, setCurrency] = useState(payment?.currency ?? PaymentCurrencies.USD);
    const [price, setPrice] = useState(payment?.amount ?? 1);
    const [required, setRequired] = useState(payment?.required);

    async function saveStrikeConfig() {
        const cfg = {
            editSecret,
            strikeHandle: username,
            currency,
            amount: price,
            required
        } as SetPaymentConfigRequest;

        await onSaveConfig(cfg)
    }

    return (
        <div>
            <dl>
                <dt>Strike username:</dt>
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
                <dd><input type="number" value={price} onChange={(e) => setPrice(Number(e.target.value))}/></dd>
                <dt>Required:</dt>
                <dd><input type="checkbox" checked={required} onChange={(e) => setRequired(e.target.checked)}/></dd>
            </dl>
            <VoidButton onClick={saveStrikeConfig} options={{showSuccess: true}}>
                Save
            </VoidButton>
        </div>
    );
}