# Chainflip Stablecoin Liquidity Provisioning Bot

> Earn yield without Impermanent Loss risks!

## Demo

[![Chainflip Stablecoin Liquidity Provisioning Bot Demo](https://img.youtube.com/vi/iaxfLvpclaQ/0.jpg)](https://www.youtube.com/watch?v=iaxfLvpclaQ "Chainflip Stablecoin Liquidity Provisioning Bot Demo")

## Description 

Chainflip provides people the ability to earn a liquidity provisioning fee of 0.05% on every swap which uses the liquidity pool the user has deposited funds in.

The way Chainflip liquidity provisioning works is by having limit and range orders in a liquidity pool. While range orders are passive in nature, they are a lower class citizen compared to limit orders. Limit orders need to be actively managed however.

This managing can be done manually via the Chainflip LP Dashboard, or automatically via the Chainflip LP API. The manual approach is still in reach for retail users but requires constant babysitting to place new orders. The automated way is too complicated for retail users.

This bot automates limit orders according to a user their price preferences, automatically placing new limit orders when balance is available.

The orders are limited to the USDT/USDC/arbUSDC stablecoin pools since they do not require tracking index prices and are low risk because there is no impermanent loss risk. A dollar for a dollar.

* Slides are available at [drive.google.com/file/d/1rjavro4P2e0Z0Im4BeMyipPK5Ra-mpb4](https://drive.google.com/file/d/1rjavro4P2e0Z0Im4BeMyipPK5Ra-mpb4)
* Presentation is available at [youtube.com/watch?v=iaxfLvpclaQ](https://youtube.com/watch?v=iaxfLvpclaQ)

## Usage

### New User (no existing Chainflip signing key)

#### Generate Keys

We start by generating a new signing key for the Chainflip LP API. This will be your LP account. Run the following command and save the **Validator Account ID** and **Seed Phrase**!

```bash
sudo docker run -v .:/etc/chainflip --entrypoint=/usr/local/bin/chainflip-cli chainfliplabs/chainflip-cli:1.4 generate-keys --path .
```

#### Funding

The following steps need to be followed to fund your LP account with FLIP, which is needed to submit transactions to the Chainflip network. These steps are explained in detail in [the Chainflip Funding documentation](https://docs.chainflip.io/validators/mainnet/funding/funding-and-bidding#adding-funds-to-your-validator-node).

> Note: The minimum funding amount for registering as a Broker or LP role is technically 1 FLIP. However, we recommend funding your accounts with at least 5 FLIP to account for transaction fees.

1. Make sure you have `$FLIP` (ERC-20) in your EVM crypto wallet.
2. Go to [Chainflip Auctions Dashboard](https://auctions.chainflip.io/) > "[**My Nodes**](https://auctions.chainflip.io/nodes)".
3. Connect your crypto wallet containing the `$FLIP`.
4. Click the button "**+ Add Node**" > You should see the "**Register new node**" dialog.
5. Enter the Validator ID you got during [the previous step](#generate-keys) — your `Validator Public Key (SS58)`— and the amount of `$FLIP` you want to fund. Click on "**Add Funds**".
6. Your crypto wallet will ask you to sign two transactions. The first one is a token approval and the second one transfers and add funds to your validator.
7. Congratulations! Your LP account is now funded, you can now jump ahead to the [All Users steps](#all-users) below.

### Existing User (has Chainflip LP signing key)

Either:
* Add your `signing_key_file` next to the `docker-compose.yml` file if you have an existing one.
* Create a `signing_key_file` containing the private key of your Chainflip LP account and make sure it has **no trailing newline**!

Proceed to the [All Users steps](#all-users) below once you have this.

### All Users

#### Creating a Telegram Group

1. Create a new group.
2. Invite `@chainflip_lp_bot` to the new group.
3. Find the group channel id, you can do this by getting the link to the group, it is the number behind the `#` sign. (For example https://web.telegram.org/k/#-1001234567890)

#### Configuring the bot

1. Open the `docker-compose.yml` file.
2. Replace `cF_YOUR_LP_ACCOUNT` with your LP account.
3. Replace `-1001234567890` with the telegram channel id.
4. If needed, configure the `_Slice` settings to how you would like to balance your funds between USDT and arbUSDC. The total needs to be **100**!
5. Save the file.
6. Run `docker compose up -d` to start the bot.
7. Run `docker compose logs -f -n 100` to follow the logs.
8. Every minute it will output the status of your balance, open orders and perform order placing activity.

#### Deposit Liquidity

1. Go to [Chainflip Liquidity Dashboard](https://lp.chainflip.io).
2. If needed, install Polkadot.js and import your Chainflip LP account.
3. Deposit USDC, USDT or arbUSDC into your account.

## Technical Information

This project uses:

* [LP API](https://docs.chainflip.io/lp/integrations/lp-api)
* [LP API RPCs](https://docs.chainflip.io/lp/integrations/lp-api#rpc-methods)
* [Node RPCs](https://docs.chainflip.io/lp/integrations/lp-rpcs)

To simplify setup for a user, Docker has been used to bundle the LP API in a preconfigured way with the public Chainflip Node RPC. Thanks to this a user only has to provide their keys to run the Liquidity Bot, and not have to set up an LP API themselves.

The RPC methods are used to retrieve a user their available undeployed balance. Based on the available balance and the configured minimum order amount it then proceeds to place a limit order.

This limit order takes into account the user their share preference between the USDT/USDC and arbUSDC/USDC pools. (solUSDC can easily be added when it launches). Additionally the limit order also respects the configured tick price for each pool.

When an order is placed, a Telegram bot informs the user about this as well.
