import { SocialButtonStyle } from "./SocialButtonStyle";

export const SocialLoginButton = (config: SocialButtonStyle) => {

    return (<button
        type="button"
        style={styles.button(config)}
        disabled={false}>
        <div>
            <div style={styles.iconContainer}>
                {config.icon(config)}
            </div>
            <div style={styles.divider} />
            <div style={styles.textContainer}>{config.text}</div>
        </div>
    </button>);
}

interface socialStyles {
    button: (config: SocialButtonStyle) => React.CSSProperties,
    iconContainer: React.CSSProperties,
    divider: React.CSSProperties,
    textContainer: React.CSSProperties,
}

const styles = {
    button: (config: SocialButtonStyle) => {
        return {
            display: 'block',
            border: 0,
            borderRadius: 3,
            boxShadow: 'rgba(0, 0, 0, 0.5) 0 1px 2px',
            color: config.color,
            background: config.background,
            cursor: 'pointer',
            fontSize: '1.2em',
            margin: '5px',
            width: 'calc(100% - 10px)',
            overflow: 'hidden',
            padding: '0 10px',
            userSelect: 'none',
            paddingTop: "10px",
            paddingBottom: "10px"
        } as React.CSSProperties
    },
    divider: {
        width: '10px',
    },
    iconContainer: {
        position: "relative",
        float: "left",
        width: "8%",
        marginTop: "3px"
    },
    textContainer: {
        position: "relative",
        float: "left",
        width: "85%",
        textAlign: "left",
        marginLeft: "5%",
        marginTop: "3px"
    }
} as socialStyles;
