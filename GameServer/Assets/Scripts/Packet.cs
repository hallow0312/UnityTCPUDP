using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


    //서버가 클라이언트에게 보내는 enum
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

 //클라이언트가 서버에게 보내는 enum
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
    //빈 패킷제작 
    public Packet() //기본 생성자 
    {
        buffer = new List<byte>(); //시작버퍼 
        readPos = 0; // Set readPos to 0
    }


    //주어진 아이디의 새로운 패킷 제작 
    public Packet(int _id) //id 기반생성자
    {
        buffer = new List<byte>(); //시작버퍼 
        readPos = 0; // Set readPos to 0

        Write(_id);//패킷 id를 버퍼에 제작 
    }

    public Packet(byte[] _data) //바이트 배열 기반생성자 
    {
        buffer = new List<byte>(); // 시작 버퍼 
        readPos = 0; // Set readPos to 0

        SetBytes(_data);
    }

    #region Functions
    //패킷의 내용을 설정하고 읽을 수 있도록 준비 
    public void SetBytes(byte[] _data) //패킷 content 
    {
        Write(_data);
        readableBuffer = buffer.ToArray();
    }


    /// 버퍼 시작시 패킷 내용 길이 삽입 
    public void WriteLength()
    {
        buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count)); // Insert the byte length of the packet at the very beginning
    }

    /// 버퍼 시작시 주어진 정수형 삽입 
    public void InsertInt(int _value)
    {
        buffer.InsertRange(0, BitConverter.GetBytes(_value)); // Insert the int at the start of the buffer
    }

    /// 배열형태의 패킷 내용 가져옴 
    public byte[] ToArray()
    {
        readableBuffer = buffer.ToArray();
        return readableBuffer;
    }
    /// 패킷 내용의 길이를 가져옴 
    public int Length()
    {
        return buffer.Count; // Return the length of buffer
    }

    /// 패킷에 포함된 읽지 않은 데이터의 길이를  가져옴  전체길이 - readpos
    public int UnreadLength()
    {
        return Length() - readPos; // Return the remaining length (unread)
    }


    //재사용가능하도록 패킷 인스턴스 리셋
    public void Reset(bool _shouldReset = true)
    {
        if (_shouldReset)
        {
            buffer.Clear(); // 버퍼 클리어 
            readableBuffer = null;
            readPos = 0; // readPos 클리어 
        }
        else
        {
            readPos -= 4; // "Unread" the last read int
        }
    }
    #endregion

    #region Write Data 
    //  ###오버라이드가 많은이유 ###
    //  1. 다양한 데이터 지원
    //  2. 편의성


    /// 패킷에 바이트 더함 
    public void Write(byte _value)
    {
        buffer.Add(_value);
    }

    /// 패킷에 바이트배열을 더함
    public void Write(byte[] _value)
    {
        buffer.AddRange(_value);
    }

    /// 패킷에 short형을 더함 
    public void Write(short _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }

    /// 패킷에 int 형을 더함 
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
    //bool 형
    public void Write(bool _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }


    //string
    public void Write(string _value)
    {
        Write(_value.Length);  //.패킷의 string의 길이를 저함 
        buffer.AddRange(Encoding.ASCII.GetBytes(_value)); // Add the string itself
    }
    #endregion

    #region Read Data

    /// 바이트 단위 읽기 
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


    //바이트 배열 
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

    //short형 
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

  //int형 
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

  //long 형
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

  //float형
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

  //bool형 
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

  //string 형
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

