using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{

    private Animator _animator = null;
    private int _HorizontalHash = 0;
    private int _VerticalHash = 0;
    private int _AttackHash = 0;

    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
        _HorizontalHash = Animator.StringToHash("Horizontal");
        _VerticalHash = Animator.StringToHash("Vertical");
        _AttackHash = Animator.StringToHash("Attack");
    }

    // Update is called once per frame
    void Update()
    {
        float xAxis = Input.GetAxis("Horizontal") * 2.32f;
        float yAxis = Input.GetAxis("Vertical") * 5.66f;

        if (Input.GetMouseButtonDown(0)) _animator.SetTrigger(_AttackHash);
        _animator.SetFloat(_HorizontalHash, xAxis, 1.0f, Time.deltaTime);
        _animator.SetFloat(_VerticalHash, yAxis, 1.0f, Time.deltaTime);
    }
}
