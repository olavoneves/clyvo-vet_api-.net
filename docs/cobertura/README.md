# Cobertura

Dois resumos, gerados por `scripts/cobertura.ps1` ou `scripts/cobertura.sh`.
Nada é excluído da medição.

- [sem-infraestrutura](sem-infraestrutura/Summary.md): a suíte de qualquer
  clone, sem banco. Os 17 testes que exigem Oracle e Mongo ficam ignorados.
- [com-infraestrutura](com-infraestrutura/Summary.md): com
  `CLYVO_TESTES_DE_INFRA=1`, contra o Oracle XE e o Mongo locais. O script
  aborta se o host do Oracle for da FIAP.
