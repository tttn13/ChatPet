import type { Meta, StoryObj } from '@storybook/react-webpack5';
import { ChatMessage } from '../components/ChatMessage';

const meta = {
  title: 'Components/ChatMessage',
  component: ChatMessage,
  parameters: {
    layout: 'centered',
  },
  decorators: [
    (Story) => (
      <div style={{ width: '600px', padding: '20px' }}>
        <Story />
      </div>
    ),
  ],
  tags: ['autodocs'],
} satisfies Meta<typeof ChatMessage>;

export default meta;
type Story = StoryObj<typeof meta>;

export const UserMessage: Story = {
  args: {
    message: {
      id: '1',
      message: 'My cat has been sneezing a lot lately. Should I be concerned?',
      response: '',
      timestamp: new Date(),
      isUser: true,
    },
  },
};

export const AssistantMessage: Story = {
  args: {
    message: {
      id: '2',
      message: '',
      response: `## Frequent Sneezing in Cats

Occasional sneezing in cats is **normal**. However, frequent sneezing could indicate:

### Common Causes:
- **Allergies** (pollen, dust, food)
- **Upper respiratory infection**
- **Environmental irritants**

### What to Monitor:
1. Discharge from eyes/nose
2. Lethargy or loss of appetite
3. Changes in behavior

> **Important**: If sneezing persists for more than a day or two, or if other symptoms appear, consult your veterinarian.

For emergency care, visit your local animal hospital immediately.`,
      timestamp: new Date(),
      isUser: false,
    },
  },
};

export const AssistantMessageWithThinking: Story = {
  args: {
    message: {
      id: '3',
      message: '',
      response: 'Frequent sneezing in cats can have several causes. Common reasons include allergies, upper respiratory infections, or environmental irritants. Watch for additional symptoms and consult your vet if it persists.',
      thinking: 'The user is asking about their cat sneezing. I need to consider common causes like allergies, infections, and irritants. I should provide helpful advice while emphasizing the importance of veterinary care for persistent symptoms.',
      timestamp: new Date(),
      isUser: false,
    },
  },
};