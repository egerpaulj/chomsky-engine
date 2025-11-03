class LlmConfig:

    def __init__(self, **kwargs):
        # Set default values
        self.model = kwargs.get('model', 'llama3.2')
        self.temperature = kwargs.get('temperature', 0.0)
        self.top_k = kwargs.get('top_k', 1)
        self.top_p = kwargs.get('top_p', None)
        self.max_tokens = kwargs.get('max_tokens', 40)
        self.repeat_penalty = kwargs.get('repeat_penalty', None)
        self.frequency_penalty = kwargs.get('frequency_penalty', None)
        self.presence_penalty = kwargs.get('presence_penalty', None)
        self.typical_p = kwargs.get('typical_p', None)
        self.num_thread = kwargs.get('num_thread', None)

    model: str
    temperature: float
    top_k: int
    top_p: float
    max_tokens: int
    repeat_penalty: float
    frequency_penalty: float
    presence_penalty: float
    typical_p: float
    num_thread: int