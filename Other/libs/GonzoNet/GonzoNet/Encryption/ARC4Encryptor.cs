﻿/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the GonzoNet.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace GonzoNet.Encryption
{
    public class ARC4Encryptor : Encryptor
    {
        public DESCryptoServiceProvider CryptoService = new DESCryptoServiceProvider();
        public byte[] EncryptionKey;

        public ARC4Encryptor(string Password)
            : base(Password)
        {
            PasswordDeriveBytes Pwd = new PasswordDeriveBytes(Encoding.ASCII.GetBytes(m_Password), 
                Encoding.ASCII.GetBytes("SALT"), "SHA1", 10);
            EncryptionKey = Pwd.GetBytes(8);
        }

        public override DecryptionArgsContainer GetDecryptionArgsContainer()
        {
            DecryptionArgsContainer DArgsContainer = new DecryptionArgsContainer();
            DArgsContainer.ARC4DecryptArgs = new ARC4DecryptionArgs();
            DArgsContainer.ARC4DecryptArgs.EncryptionKey = EncryptionKey;
            DArgsContainer.ARC4DecryptArgs.Service = CryptoService;

            return DArgsContainer;
        }

        public override byte[] FinalizePacket(byte PacketID, byte[] PacketData)
        {
            MemoryStream FinalizedPacket = new MemoryStream();
            BinaryWriter PacketWriter = new BinaryWriter(FinalizedPacket);

            PasswordDeriveBytes Pwd = new PasswordDeriveBytes(Encoding.ASCII.GetBytes(m_Password),
                Encoding.ASCII.GetBytes("SALT"), "SHA1", 10);

            MemoryStream TempStream = new MemoryStream();
            CryptoStream EncryptedStream = new CryptoStream(TempStream,
                CryptoService.CreateEncryptor(EncryptionKey, Encoding.ASCII.GetBytes("@1B2c3D4e5F6g7H8")),
                CryptoStreamMode.Write);
            EncryptedStream.Write(PacketData, 0, PacketData.Length);
            EncryptedStream.FlushFinalBlock();

            PacketWriter.Write(PacketID);
            //The length of the encrypted data can be longer or smaller than the original length,
            //so write the length of the encrypted data.
            PacketWriter.Write((ushort)((long)PacketHeaders.ENCRYPTED + TempStream.Length));
            //Also write the length of the unencrypted data.
            PacketWriter.Write((ushort)PacketData.Length);
            PacketWriter.Flush();

            PacketWriter.Write(TempStream.ToArray());
            PacketWriter.Flush();

            byte[] ReturnPacket = FinalizedPacket.ToArray();

            PacketWriter.Close();

            return ReturnPacket;
        }

        public override MemoryStream DecryptPacket(PacketStream EncryptedPacket, DecryptionArgsContainer DecryptionArgs)
        {
            MemoryStream EncryptedStream = new MemoryStream(EncryptedPacket.ToArray());

            CryptoStream CStream = new CryptoStream(EncryptedStream, CryptoService.CreateDecryptor(DecryptionArgs.ARC4DecryptArgs.EncryptionKey, 
                Encoding.ASCII.GetBytes("@1B2c3D4e5F6g7H8")), CryptoStreamMode.Read);

            byte[] DecryptedBuffer = new byte[DecryptionArgs.UnencryptedLength];
            CStream.Read(DecryptedBuffer, 0, DecryptedBuffer.Length);

            return new MemoryStream(DecryptedBuffer);
        }
    }
}
