using BlockChain.service;

namespace BlockChain.models;

public class Wallet
{
    public string publicKey { get; set; }
    public string privateKey { get; set; }

    public Wallet(CryptoService cryptoService)
    {
        var keyPair = cryptoService.GenerateKeyPair();
        publicKey = keyPair.publicKey;
        privateKey = keyPair.privateKey;
    }
}