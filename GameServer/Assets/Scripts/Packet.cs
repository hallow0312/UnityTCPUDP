using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


    //������ Ŭ���̾�Ʈ���� ������ enum
 public enum ServerPackets
 {
     welcome = 1,
     spawnPlayer,
     playerPosition,
     playerRotation,
     playerDisconnected,
     playerHp,
     playerRespawn,
     CreateItemSpawner,
     ItemSpawned,
     ItemPickedUp,
     spawnProjectile,
     projectilePosition,
     projectileExploded
}

 //Ŭ���̾�Ʈ�� �������� ������ enum
 public enum ClientPackets
 {
     welcomeReceived = 1,
     playerMovement,
     playerShoot,
     playerThrowItem
 }

public class Packet : IDisposable
{
    private List<byte> buffer;
    private byte[] readableBuffer;
    private int readPos;


    public void Write(Vector3 _value)
    {
        Write(_value.x);
        Write(_value.y);
        Write(_value.z);

    }
    public void Write(Quaternion _value)
    {
        Write(_value.x);
        Write(_value.y);
        Write(_value.z);
        Write(_value.w);
    }
    //�� ��Ŷ���� 
    public Packet() //�⺻ ������ 
    {
        buffer = new List<byte>(); //���۹��� 
        readPos = 0; // Set readPos to 0
    }


    //�־��� ���̵��� ���ο� ��Ŷ ���� 
    public Packet(int _id) //id ��ݻ�����
    {
        buffer = new List<byte>(); //���۹��� 
        readPos = 0; // Set readPos to 0

        Write(_id);//��Ŷ id�� ���ۿ� ���� 
    }

    public Packet(byte[] _data) //����Ʈ �迭 ��ݻ����� 
    {
        buffer = new List<byte>(); // ���� ���� 
        readPos = 0; // Set readPos to 0

        SetBytes(_data);
    }

    #region Functions
    //��Ŷ�� ������ �����ϰ� ���� �� �ֵ��� �غ� 
    public void SetBytes(byte[] _data) //��Ŷ content 
    {
        Write(_data);
        readableBuffer = buffer.ToArray();
    }


    /// ���� ���۽� ��Ŷ ���� ���� ���� 
    public void WriteLength()
    {
        buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count)); // Insert the byte length of the packet at the very beginning
    }

    /// ���� ���۽� �־��� ������ ���� 
    public void InsertInt(int _value)
    {
        buffer.InsertRange(0, BitConverter.GetBytes(_value)); // Insert the int at the start of the buffer
    }

    /// �迭������ ��Ŷ ���� ������ 
    public byte[] ToArray()
    {
        readableBuffer = buffer.ToArray();
        return readableBuffer;
    }
    /// ��Ŷ ������ ���̸� ������ 
    public int Length()
    {
        return buffer.Count; // Return the length of buffer
    }

    /// ��Ŷ�� ���Ե� ���� ���� �������� ���̸�  ������  ��ü���� - readpos
    public int UnreadLength()
    {
        return Length() - readPos; // Return the remaining length (unread)
    }


    //���밡���ϵ��� ��Ŷ �ν��Ͻ� ����
    public void Reset(bool _shouldReset = true)
    {
        if (_shouldReset)
        {
            buffer.Clear(); // ���� Ŭ���� 
            readableBuffer = null;
            readPos = 0; // readPos Ŭ���� 
        }
        else
        {
            readPos -= 4; // "Unread" the last read int
        }
    }
    #endregion

    #region Write Data 
    //  ###�������̵尡 �������� ###
    //  1. �پ��� ������ ����
    //  2. ���Ǽ�


    /// ��Ŷ�� ����Ʈ ���� 
    public void Write(byte _value)
    {
        buffer.Add(_value);
    }

    /// ��Ŷ�� ����Ʈ�迭�� ����
    public void Write(byte[] _value)
    {
        buffer.AddRange(_value);
    }

    /// ��Ŷ�� short���� ���� 
    public void Write(short _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    /// ��Ŷ�� int ���� ���� 
    public void Write(int _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //long
    public void Write(long _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    //float
    public void Write(float _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    //bool ��
    public void Write(bool _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }


    //string
    public void Write(string _value)
    {
        Write(_value.Length);  //.��Ŷ�� string�� ���̸� ���� 
        buffer.AddRange(Encoding.ASCII.GetBytes(_value)); // Add the string itself
    }
    #endregion

    #region Read Data

    /// ����Ʈ ���� �б� 
    public byte ReadByte(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            byte _value = readableBuffer[readPos]; // Get the byte at readPos' position
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPos += 1; // Increase readPos by 1
            }
            return _value; // Return the byte
        }
        else
        {
            throw new Exception("Could not read value of type 'byte'!");
        }
    }


    //����Ʈ �迭 
    public byte[] ReadBytes(int _length, bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            byte[] _value = buffer.GetRange(readPos, _length).ToArray(); // Get the bytes at readPos' position with a range of _length
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPos += _length; // Increase readPos by _length
            }
            return _value; // Return the bytes
        }
        else
        {
            throw new Exception("Could not read value of type 'byte[]'!");
        }
    }

    //short�� 
    public short ReadShort(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            short _value = BitConverter.ToInt16(readableBuffer, readPos); // Convert the bytes to a short
            if (_moveReadPos)
                {
                    // If _moveReadPos is true and there are unread bytes
                    readPos += 2; // Increase readPos by 2
                }
                return _value; // Return the short
            }
            else
            {
                throw new Exception("Could not read value of type 'short'!");
            }
        }

  //int�� 
  public int ReadInt(bool _moveReadPos = true)
  {
      if (buffer.Count > readPos)
      {
          // If there are unread bytes
          int _value = BitConverter.ToInt32(readableBuffer, readPos); // Convert the bytes to an int
          if (_moveReadPos)
          {
              // If _moveReadPos is true
              readPos += 4; // Increase readPos by 4
          }
          return _value; // Return the int
      }
      else
      {
          throw new Exception("Could not read value of type 'int'!");
      }
  }

  //long ��
  public long ReadLong(bool _moveReadPos = true)
  {
      if (buffer.Count > readPos)
      {
          // If there are unread bytes
          long _value = BitConverter.ToInt64(readableBuffer, readPos); // Convert the bytes to a long
          if (_moveReadPos)
          {
              // If _moveReadPos is true
              readPos += 8; // Increase readPos by 8
          }
          return _value; // Return the long
      }
      else
      {
          throw new Exception("Could not read value of type 'long'!");
      }
  }

  //float��
  public float ReadFloat(bool _moveReadPos = true)
  {
      if (buffer.Count > readPos)
      {
          // If there are unread bytes
          float _value = BitConverter.ToSingle(readableBuffer, readPos); // Convert the bytes to a float
          if (_moveReadPos)
          {
              // If _moveReadPos is true
              readPos += 4; // Increase readPos by 4
          }
          return _value; // Return the float
      }
      else
      {
          throw new Exception("Could not read value of type 'float'!");
      }
  }

  //bool�� 
  public bool ReadBool(bool _moveReadPos = true)
  {
      if (buffer.Count > readPos)
      {
          // If there are unread bytes
          bool _value = BitConverter.ToBoolean(readableBuffer, readPos); // Convert the bytes to a bool
          if (_moveReadPos)
          {
              // If _moveReadPos is true
              readPos += 1; // Increase readPos by 1
          }
          return _value; // Return the bool
      }
      else
      {
          throw new Exception("Could not read value of type 'bool'!");
      }
  }

  //string ��
  public string ReadString(bool _moveReadPos = true)
  {
      try
      {
          int _length = ReadInt(); // Get the length of the string
          string _value = Encoding.ASCII.GetString(readableBuffer, readPos, _length); // Convert the bytes to a string
          if (_moveReadPos && _value.Length > 0)
          {
              // If _moveReadPos is true string is not empty
              readPos += _length; // Increase readPos by the length of the string
          }
          return _value; // Return the string
      }
      catch
      {
          throw new Exception("Could not read value of type 'string'!");
      }
  }

  public Vector3 ReadVector3(bool _moveReadPos = true)
  {
      return new Vector3(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
  }
  public Quaternion ReadQuaternion(bool _moveReadPos = true)
  {
      return new Quaternion(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));

  }
  #endregion

  private bool disposed = false;

  protected virtual void Dispose(bool _disposing)
  {
      if (!disposed)
      {
          if (_disposing)
          {
              buffer = null;
              readableBuffer = null;
              readPos = 0;
          }

          disposed = true;
      }
  }

  public void Dispose()
  {
      Dispose(true);
      GC.SuppressFinalize(this);
  }
  
}

