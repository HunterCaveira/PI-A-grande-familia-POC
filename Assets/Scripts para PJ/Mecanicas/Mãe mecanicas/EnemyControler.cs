using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EstadoInimigo
    {
        Normal,
        ImobilizadoEIluminado
    }

    [Header("Estado Atual")]
    public EstadoInimigo estadoAtual = EstadoInimigo.Normal;

    // Script de movimento obtido automaticamente via GetComponent
    private MonoBehaviour scriptDeMovimento;

    [Header("Componentes de Física")]
    private CharacterController characterController;

    [Header("Configurações Visuais (X-Ray / Brilho através da parede)")]
    [SerializeField] private Renderer[] renderersDoInimigo; // Arraste os meshes do inimigo aqui
    [SerializeField] private Material materialXRay;        // Material especial com ZTest Always para ver pelas paredes
    private Material[] materiaisOriginais;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // OPÇÃO 1: Se o seu script de movimento tiver um nome específico (ex: "EnemyMovement"), use assim:
        // scriptDeMovimento = GetComponent<EnemyMovement>();

        // OPÇÃO 2 (Automática): Pega o primeiro outro script do GameObject que não seja este EnemyController
        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
        {
            if (script != this)
            {
                scriptDeMovimento = script;
                break; // Pega o primeiro script de movimento encontrado
            }
        }

        // Salva os materiais originais de cada parte do inimigo
        if (renderersDoInimigo != null && renderersDoInimigo.Length > 0)
        {
            materiaisOriginais = new Material[renderersDoInimigo.Length];
            for (int i = 0; i < renderersDoInimigo.Length; i++)
            {
                if (renderersDoInimigo[i] != null)
                {
                    materiaisOriginais[i] = renderersDoInimigo[i].material;
                }
            }
        }
    }

    void Update()
    {
        // Se o inimigo estiver preso pela Skill 2, ele para de executar o movimento
        if (scriptDeMovimento != null && !scriptDeMovimento.enabled && estadoAtual == EstadoInimigo.ImobilizadoEIluminado)
        {
            return;
        }

        // --- SEU CÓDIGO DE MOVIMENTO NORMAL FICA AQUI ---
    }

    // ==========================================
    // 1. SISTEMA DE EMPURRÃO (SKILL 1)
    // ==========================================

    public void ReceberEmpurrao(Vector3 direcao, float forca, float duracao)
    {
        // Só toma empurrão se estiver no estado normal e o CharacterController existir
        if (estadoAtual == EstadoInimigo.Normal && characterController != null)
        {
            StartCoroutine(AplicarEmpurraoRoutine(direcao, forca, duracao));
        }
    }

    private IEnumerator AplicarEmpurraoRoutine(Vector3 direcao, float forca, float duracao)
    {
        float tempoDecorrido = 0f;

        while (tempoDecorrido < duracao && estadoAtual == EstadoInimigo.Normal)
        {
            float fatorForca = Mathf.Lerp(forca, 0f, tempoDecorrido / duracao);
            characterController.Move(direcao * fatorForca * Time.deltaTime);

            tempoDecorrido += Time.deltaTime;
            yield return null;
        }
    }

    // ==========================================
    // 2. SISTEMA DE PRISÃO E VISÃO X-RAY (SKILL 2)
    // ==========================================

    public void AplicarEfeitoPrisao(float duracao)
    {
        StopAllCoroutines();
        StartCoroutine(EfeitoPrisaoRoutine(duracao));
    }

    private IEnumerator EfeitoPrisaoRoutine(float duracao)
    {
        // 1. Muda o estado para imobilizado
        estadoAtual = EstadoInimigo.ImobilizadoEIluminado;

        // 2. DESATIVA O SCRIPT DE MOVIMENTO OBTIDO VIA GETCOMPONENT IMEDIATAMENTE
        if (scriptDeMovimento != null)
        {
            scriptDeMovimento.enabled = false;
        }

        // 3. Ativa o visual X-Ray (brilha e atravessa paredes)
        AtivarEfeitoXRay(true);

        // 4. Aguarda o tempo da zona de comando
        yield return new WaitForSeconds(duracao);

        // 5. Restaura tudo ao normal (Reativa o script de movimento)
        AtivarEfeitoXRay(false);

        if (scriptDeMovimento != null)
        {
            scriptDeMovimento.enabled = true;
        }

        estadoAtual = EstadoInimigo.Normal;

        Debug.Log($"{gameObject.name} recuperou os movimentos e saiu do X-Ray!");
    }

    // ==========================================
    // 3. MÉTODOS AUXILIARES E DE CONSULTA
    // ==========================================

    public bool EstaImobilizado()
    {
        return estadoAtual == EstadoInimigo.ImobilizadoEIluminado;
    }

    private void AtivarEfeitoXRay(bool ativar)
    {
        if (renderersDoInimigo == null) return;

        for (int i = 0; i < renderersDoInimigo.Length; i++)
        {
            if (renderersDoInimigo[i] != null)
            {
                if (ativar && materialXRay != null)
                {
                    renderersDoInimigo[i].material = materialXRay;
                }
                else if (!ativar && materiaisOriginais[i] != null)
                {
                    renderersDoInimigo[i].material = materiaisOriginais[i];
                }
            }
        }
    }
}