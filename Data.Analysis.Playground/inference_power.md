# Local hardware
| Device                    | Gaming Use                                        | ML / Compute Use                           | Power (W) | Price CHF    | CUDA / GPU Cores | Memory         | Memory Bus / Unified | Bandwidth  | FP32 / Compute Performance                     |
| ------------------------- | ------------------------------------------------- | ------------------------------------------ | --------- | ------------ | ---------------- | -------------- | -------------------- | ---------- | ---------------------------------------------- |
| NVIDIA RTX 4060 Ti 16 GB  | Very good 1080p/1440p, decent ray‑tracing         | Entry‑level ML/AI                          | 165       | 500‑600      | 4,352            | 16 GB GDDR6    | 128‑bit              | 288 GB/s   | 22 TFLOPS                                      |
| NVIDIA RTX 4070           | Excellent 1440p/4K moderate + ray‑tracing         | Solid mid-tier ML/AI                       | 200       | 650‑750      | 5,888            | 12 GB GDDR6X   | 192‑bit              | 504 GB/s   | 40 TFLOPS (est)                                |
| NVIDIA RTX 4070 Ti        | Excellent 1440p/4K high/ultra + heavy ray‑tracing | Strong ML/AI workflows                     | 285       | 900‑1,200    | 7,680            | 12 GB GDDR6X   | 192‑bit              | 504 GB/s   | 40 TFLOPS (est)                                |
| NVIDIA RTX 5090           | Top‑end 4K/8K gaming                              | High-end ML/AI                             | 575       | 2,200‑2,600+ | 21,760           | 32 GB GDDR7    | 512‑bit              | 1,792 GB/s | 104.8 TFLOPS                                   |
| NVIDIA DGX Spark          | Not designed for gaming                           | Very strong for ML/AI & inference          | 170       | 3,782        | N/A              | 128 GB unified | N/A                  | 273 GB/s   | ~1 PFLOP (FP4, inference)                      |
| Apple Mac Studio M4 Max   | Moderate (MacOS gaming)                           | Professional ML/AI via Neural Engine + GPU | N/A       | 2,079        | 32‑core GPU      | 36 GB unified  | Unified              | 546 GB/s   | Apple ML Perf (est. FP32 ~10‑15 TFLOPS equiv.) |
| Apple Mac Studio M3 Ultra | Moderate (MacOS gaming)                           | Top-tier ML/AI via Neural Engine + GPU     | N/A       | 8,535        | 60‑core GPU      | 96 GB unified  | Unified              | 819 GB/s   | Apple ML Perf (est. FP32 ~20‑30 TFLOPS equiv.) |


# Cloud

## Current token size for local models (2000 tokens)

| Model                    | Provider     | Input Price / 1K tokens | Output Price / 1K tokens | Estimated Total Cost (500K articles) | Budget Limits |
| ------------------------ | ------------ | ----------------------- | ------------------------ | ------------------------------------ | ------------- |
| GPT-4.1                  | OpenAI/Azure | $0.0040                 | $0.0158                  | $9,900                               | Yes           |
| GPT-4.1 mini             | OpenAI/Azure | $0.0008                 | $0.0032                  | $2,000                               | Yes           |
| GPT-4.1 nano             | OpenAI/Azure | $0.0002                 | $0.0008                  | $500                                 | Yes           |
| o3                       | OpenAI/Azure | $0.0040                 | $0.0158                  | $9,900                               | Yes           |
| o4-mini                  | OpenAI/Azure | $0.0022                 | $0.0087                  | $5,450                               | Yes           |
| GPT-4o                   | OpenAI/Azure | $0.0099                 | $0.0396                  | $24,750                              | Yes           |
| GPT-o4-mini              | OpenAI/Azure | $0.0012                 | $0.0048                  | $3,000                               | Yes           |
| Claude Opus 4            | Anthropic    | $0.0297                 | $0.1485                  | $88,500                              | Yes           |
| Claude Sonnet 4          | Anthropic    | $0.0059                 | $0.0297                  | $17,800                              | Yes           |
| Claude Sonnet 3.7        | Anthropic    | $0.0059                 | $0.0297                  | $17,800                              | Yes           |
| Gemini 2.5 Flash Preview | Google       | $0.0003                 | $0.0012                  | $750                                 | Yes           |
| Gemini 2.5 Pro Preview   | Google       | $0.0025                 | $0.0198                  | $11,150                              | Yes           |
| Gemini 2.0 Flash         | Google       | $0.0002                 | $0.0008                  | $500                                 | Yes           |
| Gemini 2.0 Flash-Lite    | Google       | $0.0001                 | $0.0006                  | $350                                 | Yes           |

## Cloud - Reduced token size for cloud (800 tokens)

| Model                    | Provider     | Estimated Total Cost | Estimated Duration (days) | Budget Limits |
| ------------------------ | ------------ | -------------------- | ------------------------- | ------------- |
| GPT-4.1                  | OpenAI/Azure | $3,960               | ~92                       | Yes           |
| GPT-4.1 mini             | OpenAI/Azure | $800                 | ~37                       | Yes           |
| GPT-4.1 nano             | OpenAI/Azure | $200                 | ~18                       | Yes           |
| o3                       | OpenAI/Azure | $3,960               | ~92                       | Yes           |
| o4-mini                  | OpenAI/Azure | $2,180               | ~55                       | Yes           |
| GPT-4o                   | OpenAI/Azure | $9,900               | ~54                       | Yes           |
| GPT-o4-mini              | OpenAI/Azure | $1,200               | ~40                       | Yes           |
| Claude Opus 4            | Anthropic    | $35,400              | ~42                       | Yes           |
| Claude Sonnet 4          | Anthropic    | $7,120               | ~58                       | Yes           |
| Claude Sonnet 3.7        | Anthropic    | $7,120               | ~58                       | Yes           |
| Gemini 2.5 Flash Preview | Google       | $300                 | ~15                       | Yes           |
| Gemini 2.5 Pro Preview   | Google       | $4,460               | ~17                       | Yes           |
| Gemini 2.0 Flash         | Google       | $200                 | ~12                       | Yes           |
| Gemini 2.0 Flash-Lite    | Google       | $140                 | ~12                       | Yes           |


## Cloud - Another metric evaluation (800 tokens)
| Provider                | Model                       | Context     | Input/1k Tokens | Output/1k Tokens | Per Call | Estimated Total Cost | Est. Duration (days) |
| ----------------------- | --------------------------- | ----------- | --------------- | ---------------- | -------- | -------------------- | -------------------- |
| xAI                     | Grok 2                      | 131K        | $0.002          | $0.010           | $0.0026  | $104                 | 7                    |
| xAI                     | Grok 3                      | 131K        | $0.003          | $0.015           | $0.0039  | $156                 | 7                    |
| xAI                     | Grok 3 Mini                 | 131K        | $0.0003         | $0.0005          | $0.0003  | $11.60               | 7                    |
| OpenAI                  | GPT-5                       | 400K        | $0.00125        | $0.01            | $0.002   | $80                  | 9                    |
| OpenAI                  | o3                          | 200K/100K   | $0.01           | $0.04            | $0.012   | $480                 | 8                    |
| OpenAI                  | GPT-4.1 nano                | 1014K/32.7K | $0.0001         | $0.0004          | $0.0001  | $4.80                | 10                   |
| OpenAI                  | GPT-4.1 mini                | 1014K/32.7K | $0.0004         | $0.0016          | $0.0005  | $19.20               | 10                   |
| OpenAI                  | GPT-4.1                     | 1014K/32.7K | $0.002          | $0.008           | $0.0024  | $96                  | 10                   |
| OpenAI                  | GPT-4.5                     | 128K/16.3K  | $0.075          | $0.15            | $0.075   | $3,000               | 9                    |
| OpenAI                  | GPT-4o                      | 128K/16K    | $0.0025         | $0.01            | $0.003   | $120                 | 9                    |
| OpenAI                  | GPT-4o mini                 | 128K/16K    | $0.00015        | $0.0006          | $0.0002  | $7.20                | 9                    |
| OpenAI                  | o3-mini                     | 200K/100K   | $0.0011         | $0.0044          | $0.0013  | $52                  | 8                    |
| OpenAI                  | o1                          | 200K/100K   | $0.015          | $0.06            | $0.018   | $720                 | 8                    |
| Mistral AI              | Mistral Large 2             | 128K        | $0.000003       | $0.000009        | $0.0000  | $0.13                | 6                    |
| Mistral AI              | Mistral Small 3.1           | 128K        | $0.0000         | $0.0000          | $0.0000  | $0.00                | 6                    |
| Mistral AI              | Pixtral Large               | 128K        | $0.000002       | $0.000006        | $0.0000  | $0.09                | 6                    |
| Meta via Deepinfra/Groq | Llama 3.3 70b               | 8K/2K       | $0.00059        | $0.00079         | $0.0006  | $24                  | 8                    |
| Meta via Deepinfra      | Llama 4 Scout               | 10,000K     | $0.00008        | $0.0003          | $0.0001  | $3.76                | 7                    |
| Meta via Deepinfra      | Llama 4 Maverick            | 1,000K      | $0.00018        | $0.0006          | $0.0002  | $8                   | 7                    |
| Meta via Deepinfra      | Llama 3.3 70b               | 128K/2K     | $0.00023        | $0.0004          | $0.0002  | $8.96                | 8                    |
| Meta via Deepinfra      | Llama 3.1 405b              | 128K/2K     | $0.00179        | $0.00179         | $0.0016  | $64.4                | 8                    |
| Meta via Deepinfra      | Llama 3.2 90b               | 128K/2K     | $0.00035        | $0.0004          | $0.0003  | $12.8                | 8                    |
| Meta via Deepinfra      | Llama 3.1 70b               | 128K/2K     | $0.00023        | $0.0004          | $0.0002  | $8.96                | 8                    |
| Meta via Deepinfra      | Llama 3.2 11b               | 128K/2K     | $0.000055       | $0.000055        | $0.0000  | $1.98                | 8                    |
| Google                  | Gemini 2.5 Pro Preview      | 1000K/64K   | $0.0025         | $0.015           | $0.0035  | $140                 | 5                    |
| Google                  | Gemini 2.0 Flash            | 1000K/8K    | $0.0001         | $0.0004          | $0.0001  | $4.80                | 5                    |
| Google                  | Gemini 2.0 Flash-Lite       | 1000K/8K    | $0.000075       | $0.0003          | $0.0001  | $3.60                | 5                    |
| Google                  | Gemini 1.5 Pro              | 128K        | $0.00125        | $0.005           | $0.0015  | $60                  | 5                    |
| Google                  | Gemini 1.5 Flash            | 128K        | $0.000075       | $0.0003          | $0.0001  | $3.60                | 5                    |
| Google                  | Gemini 1.5 Flash-8B         | 128K        | $0.000037       | $0.00015         | $0.0000  | $1.80                | 5                    |
| DeepSeek                | DeepSeek-V3                 | 128K/8K     | $0.00014        | $0.00028         | $0.0001  | $5.60                | 6                    |
| DeepSeek                | DeepSeek-R1                 | 128K/8K     | $0.00055        | $0.00219         | $0.0007  | $26.36               | 6                    |
| Cohere                  | Command A                   | 256K/8K     | $0.0025         | $0.01            | $0.003   | $120                 | 7                    |
| Cohere                  | Command R7B                 | 128K/4K     | $0.000037       | $0.00015         | $0.0000  | $1.80                | 7                    |
| Cohere                  | Command R                   | 128K/4K     | $0.0005         | $0.0015          | $0.0006  | $24                  | 7                    |
| Cohere                  | Command R+                  | 128K        | $0.003          | $0.015           | $0.0039  | $156                 | 7                    |
| Cohere                  | Command                     | 4K          | $0.01           | $0.02            | $0.01    | $400                 | 7                    |
| Anthropic               | Claude Opus 4.1             | 200K        | $0.015          | $0.075           | $0.0195  | $780                 | 9                    |
| Anthropic               | Claude Opus 4               | 200K        | $0.015          | $0.075           | $0.0195  | $780                 | 9                    |
| Anthropic               | Claude 4                    | 200K        | $0.015          | $0.075           | $0.0195  | $780                 | 9                    |
| Anthropic               | Claude Sonnet 4             | 200K        | $0.003          | $0.015           | $0.0039  | $156                 | 9                    |
| Anthropic               | Claude 3 Opus               | 200K        | $0.015          | $0.075           | $0.0195  | $780                 | 9                    |
| Anthropic               | Anthropic Claude 3.7 Sonnet | 200K        | $0.003          | $0.015           | $0.0039  | $156                 | 9                    |
| Anthropic               | Claude 3.5 Sonnet           | 200K/8K     | $0.003          | $0.015           | $0.0039  | $156                 | 9                    |
| Anthropic               | Claude 3.5 Haiku            | 200K/8K     | $0.0008         | $0.004           | $0.001   | $41.6                | 9                    |
| Amazon                  | Amazon Nova Micro           | 128K        | $0.000035       | $0.00014         | $0.0000  | $1.68                | 6                    |
| Amazon                  | Amazon Nova Lite            | 300K        | $0.00006        | $0.00024         | $0.0001  | $2.88                | 6                    |
| Amazon                  | Amazon Nova Pro             | 300K        | $0.0008         | $0.0032          | $0.001   | $38.4                | 6                    |
