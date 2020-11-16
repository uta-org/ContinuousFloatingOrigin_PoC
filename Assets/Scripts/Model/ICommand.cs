using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CFO.Model
{
    public interface ICommand
    {
        void Execute(string[] args);
    }
}