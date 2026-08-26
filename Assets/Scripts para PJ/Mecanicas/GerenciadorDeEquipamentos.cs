using UnityEngine;
using UnityEngine.InputSystem;

public class GerenciadorDeEquipamentos : MonoBehaviour
{
    [Header("Estados das Habilidades")]
    [SerializeField] private bool habilitado = false;
    private int proximoIndicePendente = -1;

    [Header("Tempo de animação")]
    [SerializeField] private float tempoAnimacaoTroca = 0.05f;
    private float tempoLiberadoParaTrocar = 0f;
    private int indiceEquipamentoAtual = 0;

    [Header("Prefabs das Habilidades")]
    [SerializeField] private Transform pontosMaos;
    [SerializeField] private HabilidadeParede habilidadeParede;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Verifica se existe um teclado físico ou virtual conectado ao sistema para evitar erros de referência nula
        if (Keyboard.current != null)
        {
            // Checa se a tecla Q foi pressionada exatamente neste frame
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                // Agenda a habilidade do slot 1 (Parede) na fila de espera
                proximoIndicePendente = 1;
            }
            // Se Q não foi pressionado, checa se a tecla E foi pressionada neste frame
            else if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Agenda a habilidade do slot 2 (Tônico) na fila de espera
                proximoIndicePendente = 2;
            }
            // Se nem Q nem E foram pressionados, checa se a tecla F foi pressionada neste frame
            else if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                // Agenda a habilidade do slot 3 (Cinto) na fila de espera
                proximoIndicePendente = 3;
            }
        }

        // Avalia duas condições: se a trava de tempo/animação já passou E se existe alguma troca agendada no buffer (diferente de -1)
        if (Time.time >= tempoLiberadoParaTrocar && proximoIndicePendente != -1)
        {
            // Executa a troca passando o número da habilidade que estava aguardando na fila
            TrocarEquipamento(proximoIndicePendente);

            // Reseta o buffer para -1 para indicar que a fila está vazia e evitar trocas repetidas
            proximoIndicePendente = -1;
        }
    }

    private void TrocarEquipamento(int novoIndice)
    {
        if (habilidadeParede != null)
        { 
            habilidadeParede.CancelarPosicionamento();
        
        }

        if (novoIndice == indiceEquipamentoAtual)
        {
            novoIndice = 0;

        }

        for (int i = 0; i < pontosMaos.childCount; i++) {

            pontosMaos.GetChild(i).gameObject.SetActive(i == novoIndice); 
           
        }

        indiceEquipamentoAtual = novoIndice;

        tempoLiberadoParaTrocar = Time.time + tempoAnimacaoTroca;
    
    }
      



}
